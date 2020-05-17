using ETModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class MyTest : MonoBehaviour
{
    Queue<int> aaa = new Queue<int>();
    Queue<int> bbb = new Queue<int>();
    void Start()
    {
        //Log.Info()
        //aaa.Enqueue(1);
        //aaa.Enqueue(2);
        //aaa.Enqueue(3);
        //aaa.Enqueue(8);
        //aaa.Enqueue(9);

        //bbb.Enqueue(4);
        //bbb.Enqueue(5);
        //bbb.Enqueue(6);
        //ObjectHelper.Swap(ref aaa, ref bbb);
        //Debug.Log(aaa.Count);
        //Debug.Log(bbb.Count);
        //StartCoroutine(testc());

        TestA();
    }

    async void TestA() {
        Debug.Log($"Thread.CurrentThread.ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
        await Task.Run(TestB);

        Debug.Log($"Thread.CurrentThread.ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
        await Task.Run(TestC);
        Debug.Log($"Thread.CurrentThread.ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
    }

    async Task TestB() {
        Debug.Log($"Thread.CurrentThread.ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
        await Task.Delay(1000);
    }

    async Task TestC()
    {
        Debug.Log($"Thread.CurrentThread.ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
        await Task.Delay(1000);
    }

  



    IEnumerator testc()
    {
        yield return new WaitForSeconds(5);
       
        for (int i = 0; i < aaa.Count; i++)
        {
            Debug.Log(aaa.Dequeue());
        }
        for (int i = 0; i < bbb.Count; i++)
        {
            Debug.Log(bbb.Dequeue());
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
