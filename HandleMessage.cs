using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;

namespace FoxSayQ
{
  public class HandleMessage
  {
    //处理群组消息
    public readonly string ROBOT_QQ_NUM = "3221734968";

    public void HandleGroupMessage(GroupMessageReceiver x)
    {
      var msg = x.MessageChain.GetPlainMessage();
      Console.WriteLine($"收到了来自群{x.GroupId}由{x.Sender.Name}[{x.Sender.Id}]发送的消息：{msg}");
      Task.Run(() => HandleGroupMessageAnysc(x, msg));
    }

    private void HandleGroupMessageAnysc(GroupMessageReceiver x, string msg)
    {
      //处理At信息，绝大部分都需要
      if (x.MessageChain.Where(v => v is AtMessage atMsg && atMsg.Target == ROBOT_QQ_NUM).Any())
      {
        Console.Write($"Robot:收到指令 {msg} @");
        string[] args = msg.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
        foreach (string sub in args)
        {
          Console.Write($"|{sub}|");
        }
        Console.WriteLine();
        if (args.Length == 0)
        {
          return;
        }
        string main_arg = args[0];
        Echo.GroupMessageBehavior(x.GroupId, msg);
        // if (main_arg == "echo")
        // {
        //   Echo.GroupMessageBehavior(x.GroupId, msg);
        // }
        // else
        // {
        // }
      }
    }
  }
}