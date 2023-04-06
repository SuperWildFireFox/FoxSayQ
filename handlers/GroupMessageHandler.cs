using FoxSayQ.functions;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions.Http.Managers;

namespace FoxSayQ.handlers
{
  public class GroupMessageHandler
  {
    //机器人自身的QQ号
    public readonly string ROBOT_QQ_NUM;

    public GroupMessageHandler(String qq_num){
      this.ROBOT_QQ_NUM = qq_num;
    }


    public void Handle(GroupMessageReceiver x)
    {
      var msg = x.MessageChain.GetPlainMessage();
      Console.WriteLine($"收到了来自群{x.GroupId}由{x.Sender.Name}[{x.Sender.Id}]发送的消息：{msg}");
      Task.Run(() => HandleAnysc(x, msg));
    }

    private async Task HandleAnysc(GroupMessageReceiver x, string msg)
    {
      //前缀消息处理器
      if(msg.StartsWith("ai")){
        msg = msg.Substring("ai".Length).Trim();
        //ai的特权指令
        if(msg=="帮助"){
          string help_message = "ChatGPT使用帮助:\n" +
          "\tai <聊天内容>\t#与GPT3.5聊天\n"+
          "\tai 清空对话\t#清空历史对话\n"+
          "\tai 设置预设 <预设>\t#更新预设(同时清空对话)\n"+
          "\tai 重置预设\t#重置预设为默认\n"+
          "\tai 状态\t#显示目前ai状态\n";
          await MessageManager.SendGroupMessageAsync(x.GroupId, help_message);
          return;
        }
        ChatGPT? chat = ChatGPT.GetChatInstance(x.GroupId);
        if(chat==null){
          await MessageManager.SendGroupMessageAsync(x.GroupId, "chatgpt服务暂时不可用");
          return;
        }
        if(msg=="清空对话"){
          chat.ClearMemory();
          await MessageManager.SendGroupMessageAsync(x.GroupId, "清空对话成功");
          return;
        }
        if(msg=="重置预设"){
          chat.SetPrompt(chat.default_group_prompt);
          chat.ClearMemory();
          await MessageManager.SendGroupMessageAsync(x.GroupId, "重置预设成功");
          return;
        }
        if(msg=="状态"){
          string status_message = "当前对话状态:\n"+
          $"\t有效对话数:\t{chat.history_message.Count+1}\n"+
          $"\t当前对话消耗token数:\t{chat.status_used_token}\n"+
          $"\t当前预设:\t{chat.now_prompt}\n";
          await MessageManager.SendGroupMessageAsync(x.GroupId, status_message);
          return;
        }
        if(msg.StartsWith("设置预设")){
          string prompt = msg.Substring("设置预设".Length).Trim();
          if(chat.SetPrompt(prompt)){
            await MessageManager.SendGroupMessageAsync(x.GroupId, "新预设设置成功");
            chat.ClearMemory();
          }else{
            await MessageManager.SendGroupMessageAsync(x.GroupId, "chatgpt服务暂时不可用");
          }
          return;
        }
        await MessageManager.SendGroupMessageAsync(x.GroupId, chat.GetRespond(msg).Result);
      }

      // 处理At信息，绝大部分都需要
      // if (x.MessageChain.Where(v => v is AtMessage atMsg && atMsg.Target == ROBOT_QQ_NUM).Any())
      // {
      //   Console.Write($"Robot:收到指令 {msg} @");
      //   string[] args = msg.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
      //   foreach (string sub in args)
      //   {
      //     Console.Write($"|{sub}|");
      //   }
      //   Console.WriteLine();
      //   if (args.Length == 0)
      //   {
      //     return;
      //   }
      //   string main_arg = args[0];
      //   if(msg==" 我是傻逼"){
      //     Echo.GroupMessageBehavior(x.GroupId, "您是傻逼");
      //   }else{
      //     Echo.GroupMessageBehavior(x.GroupId, msg);
      //   }
      // }
    }
  }
}