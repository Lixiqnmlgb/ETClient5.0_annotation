using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ETModel;

namespace ETTools
{
    internal class OpcodeInfo
    {
        public string Name;
        public int Opcode;
    }

    public static class Program
    {
        public static void Main()
        {
            string protoc = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //将proto文件转化为C#脚本的程序
                protoc = "protoc.exe";
            }
            else
            {
                protoc = "protoc";
            }
            //运行protoc程序 生成C#文件
            ProcessHelper.Run(protoc, "--csharp_out=\"../Unity/Assets/Model/Module/Message/\" --proto_path=\"./\" OuterMessage.proto", waitExit: true);
            ProcessHelper.Run(protoc, "--csharp_out=\"../Unity/Assets/Hotfix/Module/Message/\" --proto_path=\"./\" HotfixMessage.proto", waitExit: true);

            // InnerMessage.proto生成InnerMessage.cs与InnerOpcode.cs
            //将内网用的proto协议 转化为C#代码
            InnerProto2CS.Proto2CS(); 

            Proto2CS("ETModel", "OuterMessage.proto", clientMessagePath, "OuterOpcode", 100);

            Proto2CS("ETHotfix", "HotfixMessage.proto", hotfixMessagePath, "HotfixOpcode", 10000);
            
            Console.WriteLine("proto2cs succeed!");
        }

        private const string protoPath = ".";
        //非热更的协议 存放的路径
        private const string clientMessagePath = "../Unity/Assets/Model/Module/Message/";
        //热更的协议 存放的路径
        private const string hotfixMessagePath = "../Unity/Assets/Hotfix/Module/Message/";

        private static readonly char[] splitChars = { ' ', '\t' };
        private static readonly List<OpcodeInfo> msgOpcode = new List<OpcodeInfo>();

        public static void Proto2CS(string ns, string protoName, string outputPath, string opcodeClassName, int startOpcode, bool isClient = true)
        {
            msgOpcode.Clear();
            string proto = Path.Combine(protoPath, protoName);

            //读取proto文件内的所有文本
            string s = File.ReadAllText(proto);

            //生成代码
            StringBuilder sb = new StringBuilder();
            sb.Append("using ETModel;\n");
            sb.Append($"namespace {ns}\n");
            sb.Append("{\n");

            bool isMsgStart = false;
            //读取proto的每一行
            foreach (string line in s.Split('\n'))
            {
                //删除头尾空白符的字符串
                string newline = line.Trim();
                //如果是空行 则遍历下一个元素
                if (newline == "")
                {
                    continue;
                }
                //如果是行注释 追加到sb中
                if (newline.StartsWith("//"))
                {
                    sb.Append($"{newline}\n");
                }
                //如果开头是message
                if (newline.StartsWith("message"))
                {
                    //要继承的父类
                    string parentClass = "";
                    isMsgStart = true;
                    //类名
                    string msgName = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)[1];
                    //按//符号进行分割
                    string[] ss = newline.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);
                    //如果能切出2个 那么父类名称就设置为"//"符号后面的字符串
                    if (ss.Length == 2)
                    {
                        parentClass = ss[1].Trim();
                    }
                    else
                    {
                        parentClass = "";
                    }
                    //存储Opcode操作码的List 添加一个元素 操作码自增
                    msgOpcode.Add(new OpcodeInfo() { Name = msgName, Opcode = ++startOpcode });
                    //添加特性  opcodeClassName是OuterOpcode或者HotfixOpcode
                    sb.Append($"\t[Message({opcodeClassName}.{msgName})]\n");
                    //使用partial关键字 可以声明一个类由多个部分构成 这里主要是为了让该类继承自:proto文件注释中写的父类
                    sb.Append($"\tpublic partial class {msgName} ");
                    if (parentClass != "")
                    {
                        sb.Append($": {parentClass} ");
                    }
                    //换行
                    sb.Append("{}\n\n");
                }
                //如果是}符号 表示已经已经处理完一条proto message了.
                if (isMsgStart && newline == "}")
                {
                    isMsgStart = false;
                }
            }

            sb.Append("}\n");

            //生成操作码 ns:命名空间 opcodeClassName:操作码类名 outputPath:输出路径 sb:生成的内容(该方法内部还会追加)
            GenerateOpcode(ns, opcodeClassName, outputPath, sb);
        }

        private static void GenerateOpcode(string ns, string outputFileName, string outputPath, StringBuilder sb)
        {
            //命名空间
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
            //类名
            sb.AppendLine($"\tpublic static partial class {outputFileName}");
            sb.AppendLine("\t{");
            //遍历所有存储的操作码
            foreach (OpcodeInfo info in msgOpcode)
            {
                //追加一条记录
                sb.AppendLine($"\t\t public const ushort {info.Name} = {info.Opcode};");
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");
            //文件输出的路径
            string csPath = Path.Combine(outputPath, outputFileName + ".cs");
            //向文件内部加入内容
            File.WriteAllText(csPath, sb.ToString());
        }
    }

    /// <summary>
    /// 服务器内部使用的proto
    /// </summary>
    public static class InnerProto2CS
    {
        private const string protoPath = ".";
        //生成后保存的路径
        private const string serverMessagePath = "../Server/Model/Module/Message/";
        private static readonly char[] splitChars = { ' ', '\t' };
        private static readonly List<OpcodeInfo> msgOpcode = new List<OpcodeInfo>();

        public static void Proto2CS()
        {
            msgOpcode.Clear();
            //通过proto生成C#  
            Proto2CS("ETModel", "InnerMessage.proto", serverMessagePath, "InnerOpcode", 1000);
            //生成操作码 最终保存到serverMessagePath路径下的InnerOpcode.cs脚本中
            GenerateOpcode("ETModel", "InnerOpcode", serverMessagePath);
        }
        
        //proto->C#
        public static void Proto2CS(string ns, string protoName, string outputPath, string opcodeClassName, int startOpcode)
        {
            msgOpcode.Clear();
            //最终生成C#脚本后 要保存的路径
            string proto = Path.Combine(protoPath, protoName);
            string csPath = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(proto) + ".cs");
            //读取proto内所有的文本
            string s = File.ReadAllText(proto);

            //存储要生成的代码
            StringBuilder sb = new StringBuilder();
            //引入的命名空间
            sb.Append("using ETModel;\n");
            sb.Append("using System.Collections.Generic;\n");
            //设置该类的命名空间
            sb.Append($"namespace {ns}\n");
            sb.Append("{\n");

            bool isMsgStart = false;
            string parentClass = "";
            //遍历Proto的每一行
            foreach (string line in s.Split('\n'))
            {
                string newline = line.Trim();

                if (newline == "")
                {
                    continue;
                }
                //如果起始是"//" 则为该类的注释 追加到sb中
                if (newline.StartsWith("//"))
                {
                    sb.Append($"{newline}\n");
                }
                //如果该行的起始是"message"
                if (newline.StartsWith("message"))
                {
                    parentClass = "";
                    isMsgStart = true;
                    string msgName = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)[1];
                    string[] ss = newline.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);
                    //获得父类名称
                    if (ss.Length == 2)
                    {
                        parentClass = ss[1].Trim();
                    }

                    //添加一个OpcodeInfo元素 主要是记录类的名称 以及自增后的Opcode
                    msgOpcode.Add(new OpcodeInfo() { Name = msgName, Opcode = ++startOpcode });

                    //添加类的特性
                    sb.Append($"\t[Message({opcodeClassName}.{msgName})]\n");
                    //将该类声明partial 该类可以由多个部分构成
                    sb.Append($"\tpublic partial class {msgName}");
                    //将设置父类的代码 压入到sb中
                    if (parentClass == "IActorMessage" || parentClass == "IActorRequest" || parentClass == "IActorResponse" ||
                        parentClass == "IFrameMessage")
                    {
                        sb.Append($": {parentClass}\n");
                    }
                    //可能是IActorLocationMessage
                    else if (parentClass != "")
                    {
                        sb.Append($": {parentClass}\n");
                    }
                    else
                    {
                        sb.Append("\n");
                    }

                    continue;
                }
                //如果已经开始遍历一个Message
                if (isMsgStart)
                {
                    //该行是"{" 则添加到sb中
                    if (newline == "{")
                    {
                        sb.Append("\t{\n");
                        continue;
                    }
                    //该行是"}" 则添加到sb中
                    if (newline == "}")
                    {
                        isMsgStart = false;
                        sb.Append("\t}\n\n");
                        continue;
                    }
                    //如果起始是 "//" 也加入进来
                    if (newline.Trim().StartsWith("//"))
                    {
                        sb.AppendLine(newline);
                        continue;
                    }
                    
                    if (newline.Trim() != "" && newline != "}")
                    {
                        //将每个message内部的每个成员转化为C#代码 追加到sb中
                        //如果起始是repeated 需要将repeated转化为List<T>
                        if (newline.StartsWith("repeated"))
                        {
                            //sb:存储要生成的代码 ns:命名空间 newline:当前遍历到的行
                            Repeated(sb, ns, newline);
                        }
                        //如果不是repeated声明的字段
                        else
                        {
                            //sb:存储要生成的代码 newline:当前遍历到的行 最后一个参数 内部没用到
                            Members(sb, newline, true);
                        }
                    }
                }
            }

            sb.Append("}\n");
            //E:\ET5.0\Server\Model\Module\Message
            //最终写入到InnerMessage.cs这个文件中
            File.WriteAllText(csPath, sb.ToString());
        }

        /// <summary>
        /// 生成操作码
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="outputFileName"></param>
        /// <param name="outputPath"></param>
        private static void GenerateOpcode(string ns, string outputFileName, string outputPath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic static partial class {outputFileName}");
            sb.AppendLine("\t{");
            //遍历每个OpcodeInfo 拿到Name和Opcode
            foreach (OpcodeInfo info in msgOpcode)
            {
                sb.AppendLine($"\t\t public const ushort {info.Name} = {info.Opcode};");
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");
            //E:\ET5.0\Server\Model\Module\Message
            //最终写入InnerOpcode.cs这个文件中
            string csPath = Path.Combine(outputPath, outputFileName + ".cs");
            File.WriteAllText(csPath, sb.ToString());
        }

        /// <summary>
        /// 追加Proto中每个Message中以repeated声明的字段 到要生成的代码中
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="ns"></param>
        /// <param name="newline"></param>
        private static void Repeated(StringBuilder sb, string ns, string newline)
        {
            try
            {
                int index = newline.IndexOf(";");//获取到";"的索引
                newline = newline.Remove(index);//移除掉";"
                
                //分割该行
                string[] ss = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                //得到类型
                string type = ss[1];
                //转换类型
                type = ConvertType(type);
                //得到字段名称
                string name = ss[2];
                //在类中声明该字段 追加到要生成的代码中
                sb.Append($"\t\tpublic List<{type}> {name} = new List<{type}>();\n\n");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{newline}\n {e}");
            }
        }

        /// <summary>
        /// proto的类型转化为C#类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string ConvertType(string type)
        {
            string typeCs = "";
            switch (type)
            {
                case "int16":
                    typeCs = "short";
                    break;
                case "int32":
                    typeCs = "int";
                    break;
                case "bytes":
                    typeCs = "byte[]";
                    break;
                case "uint32":
                    typeCs = "uint";
                    break;
                case "long":
                    typeCs = "long";
                    break;
                case "int64":
                    typeCs = "long";
                    break;
                case "uint64":
                    typeCs = "ulong";
                    break;
                case "uint16":
                    typeCs = "ushort";
                    break;
                default:
                    typeCs = type;
                    break;
            }

            return typeCs;
        }

        /// <summary>
        /// 追加Proto中每个Message的成员到要生成的代码中
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="newline"></param>
        /// <param name="isRequired"></param>
        private static void Members(StringBuilder sb, string newline, bool isRequired)
        {
            try
            {
                int index = newline.IndexOf(";");
                newline = newline.Remove(index);
                string[] ss = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                string type = ss[0];//类型
                string name = ss[1];//类名
                string typeCs = ConvertType(type);//转换后的类型 C#中能用的

                sb.Append($"\t\tpublic {typeCs} {name} {{ get; set; }}\n\n");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{newline}\n {e}");
            }
        }
    }
}
