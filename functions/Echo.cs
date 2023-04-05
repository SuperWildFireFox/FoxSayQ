using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mirai.Net.Sessions.Http.Managers;

namespace FoxSayQ.functions
{
  public class Echo
  { 
    public static async void GroupMessageBehavior(string group_id,string msg){
      await MessageManager.SendGroupMessageAsync(group_id, msg);
    }
  }
}