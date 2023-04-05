using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FoxSayQ.utils
{
#pragma warning disable CS8600, CS8602, CS8618
  public class Config
  {
    public static T ReadConfig<T>(string file_path)
    {
      using (StreamReader r = new StreamReader(file_path))
      {
        string json = r.ReadToEnd();
        dynamic json_obj = JsonConvert.DeserializeObject(json);
        T config = JsonConvert.DeserializeObject<T>(json_obj.ToString());
        return config;
      }
    }
  }

  public class MainConfig
  {
    public class RobotConfig
    {
      public String mirai_address;
      public String robot_qq_num;
      public String mirai_vertify;
    }

    public class MysqlConfig
    {

    }

    public class ProxySetting
    {
      public String http_proxy;
      public String https_proxy;
    }

    [JsonProperty("robot_setting")]
    public RobotConfig robotConfig;
    [JsonProperty("mysql_setting")]
    public MysqlConfig mysqlConfig;
    [JsonProperty("proxy_setting")]
    public ProxySetting proxySetting;
  }

  public class ChatGPTConfig{
    public bool use_proxy;
    public String openai_api;
    public String model_name;
    public String api_key;
    public int temperature;
  }




#pragma warning restore CS8600, CS8602
}