using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FoxSayQ.utils;
using Newtonsoft.Json;

namespace FoxSayQ.functions
{
  //服务器存储对话的struct
  public struct GPTMessage
  {
    public String content; //对话内容
    public int token; //token数
    public string role;

    public GPTMessage(String content, string role)
    {
      this.content = content;
      this.role = role;
      this.token = -1;
    }
  }
  //json辅助类
#pragma warning disable CS8618
  public class MessageJson
  {
    public string role;
    public string content;
  }
  public class Response
  {
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("created")]
    public int Created { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
    [JsonProperty("usage")]
    public ResponseUsage Usage { get; set; }
    [JsonProperty("choices")]
    public ResponseChoice[] Choices { get; set; }
  }

  public class ResponseUsage
  {
    [JsonProperty("prompt_tokens")]
    public int PromptTokens { get; set; }
    [JsonProperty("completion_tokens")]
    public int CompletionTokens { get; set; }
    [JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }
  }

  public class ResponseChoice
  {
    [JsonProperty("message")]
    public ResponseMessage Message { get; set; }
    [JsonProperty("finish_reason")]
    public string FinishReason { get; set; }
    [JsonProperty("index")]
    public int Index { get; set; }
  }

  public class ResponseMessage
  {
    [JsonProperty("role")]
    public string Role { get; set; }
    [JsonProperty("content")]
    public string Content { get; set; }
  }
#pragma warning restore CS8618
  public class ChatGPT
  {
    //TODO:暂时只考虑群
    private static Dictionary<string, ChatGPT> chatgpt_instance_dict = new Dictionary<string, ChatGPT>();
    private static ChatGPTConfig chatGPTConfig = Config.ReadConfig<ChatGPTConfig>("configs/chatgpt_config.json");

    public static ChatGPT? GetChatInstance(String group_number)
    {
      if (chatgpt_instance_dict.ContainsKey(group_number))
      {
        return chatgpt_instance_dict[group_number];
      }
      else
      {
        ChatGPT instance = new ChatGPT(group_number);
        if(instance.isActivate==false){
          return null;
        }
        chatgpt_instance_dict.Add(group_number, instance);
        return instance;
      }
    }

    //最大令牌数
    public readonly static int GPT_3_5_TOKEN_LIMIT = 4096;

    //默认预设
    public string default_group_prompt = "你现在是一个qq聊天群管理员，你所在的qq群号是<qq群号>。接下来，群内的人会问你各种问题，请尽力帮助他们解答，请你准备好。";
    public int default_group_prompt_token=-1;
    //token超出时回撤的记忆区长度
    public int shrink_memory_length = 3 * 2;

    //token临界比例
    public int token_threshold = (int)(0.75*GPT_3_5_TOKEN_LIMIT);

    //当前预设
    public string now_prompt = "";

    //当前token总数
    public int status_used_token;

    //当前预设token数
    private int now_prompt_token;

    //用双向链表保存对话
    public LinkedList<GPTMessage> history_message = new LinkedList<GPTMessage>();

    //HttpClient示例，我不确定长连接会不会导致问题
    private HttpClient client;

    //服务是否初始化成功
    public bool isActivate = false;

    public ChatGPT(String group_number)
    {
      default_group_prompt = default_group_prompt.Replace("<qq群号>", group_number);
      if (chatGPTConfig.use_proxy)
      {
        var proxy = new WebProxy(Program.main_config.proxySetting.http_proxy);
        var httpClientHandler = new HttpClientHandler
        {
          Proxy = proxy,
          UseProxy = true
        };
        client = new HttpClient(httpClientHandler);

      }
      else
      {
        client = new HttpClient();
      }
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", chatGPTConfig.api_key);
      //先尝试获取默认的预设token长度，以此检查链接是否正常
      if(SetPrompt(default_group_prompt)){
        isActivate = true;
        default_group_prompt_token = now_prompt_token;
      }
    }


    public bool SetPrompt(String new_prompt){
      if(new_prompt==default_group_prompt&&default_group_prompt_token!=-1){
        now_prompt = default_group_prompt;
        now_prompt_token = default_group_prompt_token;
        return true;
      }
      int tokens = CalculatePromptToken(new_prompt).Result;
      if(tokens!=-1){
        now_prompt = new_prompt;
        now_prompt_token = tokens;
        return true;
      }
      return false;
    }

    private async Task<int> CalculatePromptToken(String prompt){
      var requestData = new
      {
        model = chatGPTConfig.model_name,
        messages = new MessageJson[]{
          new MessageJson{role = "system", content = prompt}
        }
      };
      string jsonRequest = JsonConvert.SerializeObject(requestData);
      HttpResponseMessage response = await client.PostAsync(chatGPTConfig.openai_api,
        new StringContent(jsonRequest, Encoding.UTF8, "application/json"));
      if (!response.IsSuccessStatusCode)
      {
        return -1;
      }
      string jsonResponseData = await response.Content.ReadAsStringAsync();
      var responseData = JsonConvert.DeserializeObject<Response>(jsonResponseData);
      if (responseData == null)
      {
        return -1;
      }
      return responseData.Usage.PromptTokens;
    }

    //根据用户问题，返回响应
    public async Task<String> GetRespond(String new_msg)
    {
      //用户本次对话生成的存储消息体
      GPTMessage user_msg = new GPTMessage(new_msg, "user");

      //积累token数，用以计算本次对话token
      int cost_tokens = now_prompt_token;

      //开始构建消息请求
      List<MessageJson> messageJsons = new List<MessageJson>();
      messageJsons.Add(new MessageJson { role = "system", content = now_prompt });
      foreach (GPTMessage gmsg in history_message)
      {
        messageJsons.Add(new MessageJson { role = gmsg.role, content = gmsg.content });
        cost_tokens+=gmsg.token;
      }
      messageJsons.Add(new MessageJson { role = user_msg.role, content = user_msg.content });
      var requestData = new
      {
        model = chatGPTConfig.model_name,
        messages = messageJsons
      };
      string jsonRequest = JsonConvert.SerializeObject(requestData);

      //等待消息回应
      HttpResponseMessage response = await client.PostAsync(chatGPTConfig.openai_api,
        new StringContent(jsonRequest, Encoding.UTF8, "application/json"));
      if (!response.IsSuccessStatusCode)
      {
        return ErrorCodes.ChatGPTErrorCode.ERROR_API_REQUEST_FAILED + response.ReasonPhrase;
      }
      string jsonResponseData = await response.Content.ReadAsStringAsync();
      var responseData = JsonConvert.DeserializeObject<Response>(jsonResponseData);
      if (responseData == null)
      {
        return ErrorCodes.ChatGPTErrorCode.ERROR_EMPTY_RESPOND_OBJECT;
      }
      string respond_msg = responseData.Choices[0].Message.Content;

      int prompt_tokens = responseData.Usage.PromptTokens;
      int completion_tokens = responseData.Usage.CompletionTokens;
      int total_tokens = responseData.Usage.TotalTokens;
      //记录状态，展示用
      status_used_token = total_tokens;
      Debug.Assert(now_prompt_token > 0, "now_prompt_token 不能小于0");
      //读取预设长度
      user_msg.token = prompt_tokens-cost_tokens;

      //assistant的回复
      GPTMessage assistant_msg = new GPTMessage(respond_msg, "assistant");
      assistant_msg.token = completion_tokens;

      history_message.AddLast(user_msg);
      history_message.AddLast(assistant_msg);

      if(total_tokens>token_threshold){
        ShrinkLinkedList(shrink_memory_length,token_threshold);
      }
      return respond_msg;
    }

    public void ShrinkLinkedList(int n,int token_threshold){
      LinkedList<GPTMessage> new_list = new LinkedList<GPTMessage>();
      var node = history_message.Last;
      int token_sum = 0;
      while(n--!=0){
        if(node==null){
          break;
        }
        new_list.AddFirst(node.Value);
        token_sum+=node.Value.token;
        node = node.Previous;
      }
      if(token_sum>=token_threshold){
        new_list.Clear();
        #pragma warning disable CS8602
        if(history_message.Count>2){
          new_list.AddFirst(history_message.Last.Value);
          new_list.AddFirst(history_message.Last.Previous.Value);
        }
        #pragma warning restore CS8602
      }
      history_message = new_list;
    }
    public void ClearMemory(){
      history_message.Clear();
    }
  }
}
