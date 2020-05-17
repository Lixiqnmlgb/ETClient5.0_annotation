using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ETModel;
using MongoDB.Bson;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 单元格信息
/// </summary>
public struct CellInfo
{
	public string Type;//类型
	public string Name;//名称
	public string Desc;//描述
}

public class ExcelMD5Info
{
	//md5缓存信息
	public Dictionary<string, string> fileMD5 = new Dictionary<string, string>();
	//根据文件名称 获取md5
	public string Get(string fileName)
	{
		string md5 = "";
		this.fileMD5.TryGetValue(fileName, out md5);
		return md5;
	}
	//根据文件名称 添加md5
	public void Add(string fileName, string md5)
	{
		this.fileMD5[fileName] = md5;
	}
}

public class ExcelExporterEditor : EditorWindow
{
	[MenuItem("Tools/导出配置")]
	private static void ShowWindow()
	{
		GetWindow(typeof(ExcelExporterEditor));
	}

	private const string ExcelPath = "../Excel";
	private const string ServerConfigPath = "../Config/";

	//是否客户端
	private bool isClient;

	private ExcelMD5Info md5Info;
	
	// Update is called once per frame
	private void OnGUI()
	{
		try
		{
			const string clientPath = "./Assets/Res/Config";

			if (GUILayout.Button("导出客户端配置"))
			{
				//标记为客户端
				this.isClient = true;
				//将所有表格导出生成json文件 配置生成到clientPath路径下
				ExportAll(clientPath);
				//导出用于Model下的实体类
				ExportAllClass(@"./Assets/Model/Module/Demo/Config", "namespace ETModel\n{\n");
				//导出用于Hotfix下的实体类
				ExportAllClass(@"./Assets/Hotfix/Module/Demo/Config", "using ETModel;\n\nnamespace ETHotfix\n{\n");
				
				Log.Info($"导出客户端配置完成!");
			}

			if (GUILayout.Button("导出服务端配置"))
			{
				//标记为服务器
				this.isClient = false;
				//将所有表格导出生成json文件 配置生成到ServerConfigPath路径下
				ExportAll(ServerConfigPath);
				
				ExportAllClass(@"../Server/Model/Module/Demo/Config", "namespace ETModel\n{\n");
				
				Log.Info($"导出服务端配置完成!");
			}
        }
		catch (Exception e)
		{
			Log.Error(e);
		}
	}
	/// <summary>
	/// 生成所有实体类
	/// </summary>
	/// <param name="exportDir">生成的路径</param>
	/// <param name="csHead">脚本头部信息:引入的命名空间 与设定的命名空间 </param>
	private void ExportAllClass(string exportDir, string csHead)
	{//遍历存储表格的文件夹
		foreach (string filePath in Directory.GetFiles(ExcelPath))
		{//如果文件扩展名不是.xlsx 说明不是表格文件 继续遍历下一个
			if (Path.GetExtension(filePath) != ".xlsx")
			{
				continue;
			}
			//如果起始是~ 说明是缓存文件 继续遍历下一个
			if (Path.GetFileName(filePath).StartsWith("~"))
			{
				continue;
			}
			//如果是.xlsx表格文件 则导出实体类 filePath:文件路径 exportDir:导出路径 csHead:头部文本
			ExportClass(filePath, exportDir, csHead);
			Log.Info($"生成{Path.GetFileName(filePath)}类");
		}
		AssetDatabase.Refresh();
	}
	/// <summary>
	/// 导出实体类
	/// </summary>
	/// <param name="fileName">文件路径</param>
	/// <param name="exportDir">导出路径</param>
	/// <param name="csHead">头部文本</param>
	private void ExportClass(string fileName, string exportDir, string csHead)
	{
		XSSFWorkbook xssfWorkbook;
		using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{//操作表格的对象
			xssfWorkbook = new XSSFWorkbook(file);
		}
		//文件名称 不带后缀
		string protoName = Path.GetFileNameWithoutExtension(fileName);
		//生成的文件路径
		string exportPath = Path.Combine(exportDir, $"{protoName}.cs");
		using (FileStream txt = new FileStream(exportPath, FileMode.Create))
		using (StreamWriter sw = new StreamWriter(txt))
		{
			StringBuilder sb = new StringBuilder();
			//获取第一张工作表
			ISheet sheet = xssfWorkbook.GetSheetAt(0);
			//以下要生成的具体格式 参考UnitCOnfig脚本
			//追加脚本头部:引入的命名空间 与 设定的命名空间
			sb.Append(csHead);
			//设定的特性
			sb.Append($"\t[Config((int)({GetCellString(sheet, 0, 0)}))]\n");
			//类名 和 继承的父类
			sb.Append($"\tpublic partial class {protoName}Category : ACategory<{protoName}>\n");
			sb.Append("\t{\n");
			sb.Append("\t}\n\n");
			//另一个类 与 继承的父类 ...
			sb.Append($"\tpublic class {protoName}: IConfig\n");
			sb.Append("\t{\n");
			//添加共有字段 Id
			sb.Append("\t\tpublic long Id { get; set; }\n");
			//获取第四行的单元格数量
			int cellCount = sheet.GetRow(3).LastCellNum;
			//遍历每一列
			for (int i = 2; i < cellCount; i++)
			{//获取字段描述 第三行 第i列
				string fieldDesc = GetCellString(sheet, 2, i);
				//如果是带# 忽略 继续下一个
				if (fieldDesc.StartsWith("#"))
				{
					continue;
				}

				// s开头表示这个字段是服务端专用 如果当前是生成客户端的实体类 就忽略掉该字段
				if (fieldDesc.StartsWith("s") && this.isClient)
				{
					continue;
				}
				//获取字段名称 第四行 第i列 单元格的字符串
				string fieldName = GetCellString(sheet, 3, i);
				//如果字段名称是Id或者_id 则继续下一列的遍历 因为上面已经添加过了
				if (fieldName == "Id" || fieldName == "_id")
				{
					continue;
				}
				//获取字段类型 第五行第i列的单元格的字符串 如果是空 继续下一列的遍历
				string fieldType = GetCellString(sheet, 4, i);
				if (fieldType == "" || fieldName == "")
				{
					continue;
				}
				//追加一行 字段类型 字段名称;
				sb.Append($"\t\tpublic {fieldType} {fieldName};\n");
			}

			sb.Append("\t}\n");
			sb.Append("}\n");
			//写入到文件中
			sw.Write(sb.ToString());
		}
	}

	/// <summary>
	/// 导出所有
	/// </summary>
	/// <param name="exportDir"></param>
	private void ExportAll(string exportDir)
	{
		//md5文件的路径
		string md5File = Path.Combine(ExcelPath, "md5.txt");
		//如果不包含md5文件 构建ExcelMD5Info
		if (!File.Exists(md5File))
		{
			this.md5Info = new ExcelMD5Info();
		}
		else
		{
			//如果包含 就读取文件的所有文本 为json格式
			//再反序列化成实体类ExcelMD5Info
			this.md5Info = MongoHelper.FromJson<ExcelMD5Info>(File.ReadAllText(md5File));
		}
		//遍历"../Excel"路径下的所有文件
		foreach (string filePath in Directory.GetFiles(ExcelPath))
		{
			//如果扩展名不是.xlsx 继续遍历下一个元素
			if (Path.GetExtension(filePath) != ".xlsx")
			{
				continue;
			}
			//临时缓存文件 忽略之
			if (Path.GetFileName(filePath).StartsWith("~"))
			{
				continue;
			}
			//如果是.xlsx文件 
			//获取文件名 
			string fileName = Path.GetFileName(filePath);
			//旧的md5码 
			string oldMD5 = this.md5Info.Get(fileName);
			//当前的md5码
			string md5 = MD5Helper.FileMD5(filePath);
			//缓存起来
			this.md5Info.Add(fileName, md5);
			//对比md5 如果一样 则继续检查下一个元素
			if (md5 == oldMD5)
			{
				continue;
			}
			//如果不一样 则调用Export接口 filePath:文件路径 exportDir导出路径,客户端为clientPath变量的值
			Export(filePath, exportDir);
		}

		//全部遍历完 就将md5Info序列化成json 写入到md5File文件中
		File.WriteAllText(md5File, this.md5Info.ToJson());

		Log.Info("所有表导表完成");
		AssetDatabase.Refresh();
	}
	/// <summary>
	/// 导出
	/// </summary>
	/// <param name="fileName">文件名</param>
	/// <param name="exportDir">配置导出的文件夹</param>
	private void Export(string fileName, string exportDir)
	{
		XSSFWorkbook xssfWorkbook;
		//根据文件名 获取到文件流
		using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			//构建XSSFWorkbook对象 用于操作表格
			xssfWorkbook = new XSSFWorkbook(file);
		}
		//获取文件名 不带扩展名(后缀)
		string protoName = Path.GetFileNameWithoutExtension(fileName);
		Log.Info($"{protoName}导表开始");
		//导出的文件路径
		string exportPath = Path.Combine(exportDir, $"{protoName}.txt");
		//创建文件流
		using (FileStream txt = new FileStream(exportPath, FileMode.Create))
		using (StreamWriter sw = new StreamWriter(txt))
		{//遍历工作表
			for (int i = 0; i < xssfWorkbook.NumberOfSheets; ++i)
			{//获取到一张表
				ISheet sheet = xssfWorkbook.GetSheetAt(i);
				//导出表 通过StreamWriter向导出的文件写入文本
				ExportSheet(sheet, sw);
			}
		}
		Log.Info($"{protoName}导表完成");
	}
	/// <summary>
	/// 根据表格导出配置
	/// </summary>
	/// <param name="sheet">工作表</param>
	/// <param name="sw">文件写入流</param>
	private void ExportSheet(ISheet sheet, StreamWriter sw)
	{
		//获取第4行 最后一个单元格编号 得到列数
		int cellCount = sheet.GetRow(3).LastCellNum;
		//存储单元格的数组 有多少列就需要存储多少个
		CellInfo[] cellInfos = new CellInfo[cellCount];
		//遍历每一列 从第三列开始
		for (int i = 2; i < cellCount; i++)
		{
			string fieldDesc = GetCellString(sheet, 2, i);//获取第三行每一个单元格的字符串=>描述
			string fieldName = GetCellString(sheet, 3, i);//获取第四行每一个单元格的字符串=>变量名称
			string fieldType = GetCellString(sheet, 4, i);//获取第五行每一个单元格的字符串=>类型
			
			//缓存起来
			cellInfos[i] = new CellInfo() { Name = fieldName, Type = fieldType, Desc = fieldDesc };
		}
		//从第六行开始,遍历到最后一行 
		//每遍历一行 获得一条json数据 写入文件一行
		for (int i = 5; i <= sheet.LastRowNum; ++i)
		{
			//如果该行第三列的文本为空 则继续下一行的遍历
			if (GetCellString(sheet, i, 2) == "")
			{
				continue;
			}
			//要生成的文本 都存储在这里面
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			//获取该行
			IRow row = sheet.GetRow(i);
			//遍历每一列 从第三列开始
			for (int j = 2; j < cellCount; ++j)
			{
				//先判断该列的描述字段 是否加# 或者s 或者c
				string desc = cellInfos[j].Desc.ToLower();
				//忽略该列 读取下一列
				if (desc.StartsWith("#"))
				{
					continue;
				}

				// s开头表示这个字段是服务端专用 但如果现在是生成客户端配置 则忽略该列 读取下一列
				if (desc.StartsWith("s") && this.isClient)
				{
					continue;
				}

				// c开头表示这个字段是客户端专用 但如果现在是生成服务器配置 则忽略该列 读取下一列
				if (desc.StartsWith("c") && !this.isClient)
				{
					continue;
				}
				//获取该行该列的单元格文本 
				string fieldValue = GetCellString(row, j);
				//如果等于空 抛出异常
				if (fieldValue == "")
				{
					throw new Exception($"sheet: {sheet.SheetName} 中有空白字段 {i},{j}");
				}
				//如果是第三列之后的 往sb加入","符号
				if (j > 2)
				{
					sb.Append(",");
				}
				//获取到该列的字段名称
				string fieldName = cellInfos[j].Name;
				//如果字段名称是Id或者_id
				if (fieldName == "Id" || fieldName == "_id")
				{
					if (this.isClient)
					{//客户端统一用"Id"作为变量名称
						fieldName = "Id";
					}
					else
					{//服务器统一用"_id"作为变量名称
						fieldName = "_id";
					}
				}
				//获取字段类型
				string fieldType = cellInfos[j].Type;
				//按json格式添加"字段名称":"值(根据字段类型与单元格的值进行生成的)"
				sb.Append($"\"{fieldName}\":{Convert(fieldType, fieldValue)}");
			}
			sb.Append("}");
			//写入到文件中
			sw.WriteLine(sb.ToString());
		}
	}
	/// <summary>
	/// 根据字段类型与单元格的值 生成要保存的文本
	/// </summary>
	/// <param name="type">字段类型</param>
	/// <param name="value">单元格的值</param>
	/// <returns></returns>
	private static string Convert(string type, string value)
	{
		switch (type)
		{
			case "int[]":
			case "int32[]":
			case "long[]":
				return $"[{value}]";
			case "string[]":
				return $"[{value}]";
			case "int":
			case "int32":
			case "int64":
			case "long":
			case "float":
			case "double":
				return value;
			case "string":
				return $"\"{value}\"";
			default:
				throw new Exception($"不支持此类型: {type}");
		}
	}
	/// <summary>
	/// 获取单元格的字符串
	/// </summary>
	/// <param name="sheet">表</param>
	/// <param name="i">行</param>
	/// <param name="j">列</param>
	/// <returns></returns>
	private static string GetCellString(ISheet sheet, int i, int j)
	{
		return sheet.GetRow(i)?.GetCell(j)?.ToString() ?? "";
	}
	/// <summary>
	/// 获取某行某一列的数据
	/// </summary>
	/// <param name="row">行数据</param>
	/// <param name="i">第几列</param>
	/// <returns></returns>
	private static string GetCellString(IRow row, int i)
	{
		return row?.GetCell(i)?.ToString() ?? "";
	}
	/// <summary>
	/// 获取单元格的字符串
	/// </summary>
	/// <param name="cell">单元格</param>
	/// <returns></returns>
	private static string GetCellString(ICell cell)
	{
		return cell?.ToString() ?? "";
	}
}
