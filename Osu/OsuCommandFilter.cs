using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sync;
using Sync.MessageFilter;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ExtraToolsPlugin.Osu
{
    internal class OsuCommandFilter : IFilter, ISourceClient
    {
        private const string COMMAND_SHARE_BEATMAP_DL_LINK = "?share_dl";
        private const string CONST_ACTION_FLAG = "\x0001ACTION ";
        private const string GEN_DL_LINK_URL = "http://mikirasora.moe/api/osu/gen_dl_link?beatmap_setid={0}&api={1}";
        private readonly Setting setting;
        private readonly static Logger<OsuCommandFilter> logger = new Logger<OsuCommandFilter>();

        Regex beatmap_id_regex = new Regex(@"osu.ppy.sh/b/(\d+)");

        int beatmap_id=-2857;

        public OsuCommandFilter(Setting setting)
        {
            this.setting = setting;
        }

        public void onMsg(ref IMessageBase msg)
        {
            if (msg.Message.RawText.StartsWith(CONST_ACTION_FLAG))
            {
                var result = beatmap_id_regex.Match(msg.Message.RawText);
                if (result.Success)
                {
                    msg.Cancel = true;
                    var prev = beatmap_id;//backup

                    if ((!int.TryParse(result.Groups[1].Value, out beatmap_id)) || beatmap_id < 0)
                        beatmap_id = prev;//parse error and recovery

                    logger.LogInfomation($"Catch value {result.Groups[1].Value} ,now beatmap_id = {beatmap_id}");
                }
            }
            else if (msg.Message.RawText.StartsWith(COMMAND_SHARE_BEATMAP_DL_LINK))
            {
                var param = msg.Message.RawText.Remove(0, COMMAND_SHARE_BEATMAP_DL_LINK.Length).Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    msg.Cancel = true;

                    if (param.Length == 0)
                        ShareBeatmapDownloadLink(beatmap_id);
                    else if (param.Length == 2 && param[0] == "-b") //todo 即将支持beatmap_set id
                        if (int.TryParse(param[1], out int id))
                            ShareBeatmapDownloadLink(id);
                        else
                            throw new Exception($"Cant parse share command param : {param[0]} {param[1]}");
                    
                    SyncHost.Instance.ClientWrapper.Client.SendMessage(new IRCMessage(SyncHost.Instance.ClientWrapper.Client.NickName, "[OsuCommandFilter]Copied!"));
                }
                catch (Exception e)
                {
                    logger.LogError(e.Message);
                }
            }
        }

        private void ShareBeatmapDownloadLink(int beatmap_id)
        {
            if (string.IsNullOrWhiteSpace(setting.MikiraSoraAPIKey))
                throw new Exception("No mikirasora.moe api key! please apply new :http://mikirasora.moe/account/api");

            int beatmap_setid = GetBeatmapSetID(beatmap_id);

            string url = string.Format(GEN_DL_LINK_URL, beatmap_setid, setting.MikiraSoraAPIKey);
            HttpWebRequest request = HttpWebRequest.CreateHttp(url);

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                string json_str = reader.ReadToEnd();
                var json_obj = JsonConvert.DeserializeObject(json_str) as JObject;

                if (!bool.Parse(json_obj["result"].ToString()))
                    throw new Exception($"Cant get share link from beatmap id:{beatmap_id}");

                var share_link= json_obj["content"]["link"].ToString();

                SetCopyText(share_link);
            }
        }

        private int GetBeatmapSetID(int beatmap_id)
        {
            if (string.IsNullOrWhiteSpace(setting.OsuAPIKey))
                throw new Exception("No osu! api key! please get a key at :https://osu.ppy.sh/p/api");

            string url = $"https://osu.ppy.sh/api/get_beatmaps?k={setting.OsuAPIKey}&b={beatmap_id}&limit=1";
            HttpWebRequest request = WebRequest.CreateHttp(url);
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            using (StreamReader reader=new StreamReader(response.GetResponseStream()))
            {
                string json_str = reader.ReadToEnd();
                var json_array=JsonConvert.DeserializeObject(json_str) as JArray;

                if (json_array.Count() == 0)
                    throw new Exception($"Cant get beatmap setid from beatmap id:{beatmap_id}");

                return int.Parse(json_array[0]["beatmapset_id"].ToString());
            }
        }
        
        private void SetCopyText(string text)
        {
            var thread = new Thread(_thread_SetCopyText);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start(text);
            thread.Join();

            logger.LogInfomation("Copy link successfully:" + text);
        }

        private void _thread_SetCopyText(object text)
        {
            Clipboard.SetText(text.ToString(), TextDataFormat.Text);
        }
    }
}
