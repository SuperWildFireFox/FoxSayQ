using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoxSayQ.functions.manager;
using Mirai.Net.Data.Messages.Receivers;

namespace FoxSayQ.handlers
{
  public class FriendMessageHandler
  {
    public readonly string ROBOT_QQ_NUM;

    public FriendMessageHandler(String qq_num)
    {
      this.ROBOT_QQ_NUM = qq_num;
    }

    public void Handle(FriendMessageReceiver x)
    {
      var msg = x.MessageChain.GetPlainMessage();
      Console.WriteLine($"收到了来自好友{x.FriendName}({x.FriendId})发送的消息：{msg}");
      Task.Run(() => HandleAnysc(x, msg));
    }

    private async Task HandleAnysc(FriendMessageReceiver x, string msg)
    {
      if(await GroupInviteManager.Instance.CheckIfHandleAsync(x.FriendId,msg)){
        return;
      }
    }
  }
}