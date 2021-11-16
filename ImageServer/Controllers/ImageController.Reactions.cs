using ImageServer.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageServer.Controllers
{
    public partial class ImageController
    {
        [HttpPost]
        [Route("{id}/reactions/{reaction}")]
        public async Task<ActionResult<object[]>> AddReactions([FromRoute(Name = "group")] string groupName, [FromRoute] string id, [FromRoute] string reaction, [FromBody] object key)
        {
            if (Config.TryGetGroup(groupName, out Group group))
            {
                if (group.TryGetImage(id, out Image image))
                {
                    List<object> reactions;

                    if (image.Reactions.ContainsKey(reaction))
                    {
                        reactions = image.Reactions[reaction].ToList();
                    }
                    else
                    {
                        reactions = new List<object>();
                    }

                    object value = null;

                    var parsed = (JsonElement)key;

                    switch (parsed.ValueKind)
                    {
                        case JsonValueKind.String:
                            value = parsed.GetString();
                            break;
                        case JsonValueKind.Number:
                            value = parsed.GetUInt64();
                            break;
                    }

                    reactions.Add(value);

                    await Config.UpdateImage(image, new string[] { "Reactions", reaction }, new JArray(reactions));

                    return reactions.ToArray();
                }
            }

            return NotFound();
        }

        [HttpPut]
        [Route("{id}/reactions/{reaction}")]
        public async Task<ActionResult<object[]>> ReplaceReactions([FromRoute(Name = "group")] string groupName, [FromRoute] string id, [FromRoute] string reaction, [FromBody] object[] reactions)
        {
            if (Config.TryGetGroup(groupName, out Group group))
            {
                if (group.TryGetImage(id, out Image image))
                {
                    List<object> value = new List<object>();

                    foreach (JsonElement item in reactions)
                    {
                        switch (item.ValueKind)
                        {
                            case JsonValueKind.String:
                                value.Add(item.GetString());
                                break;
                            case JsonValueKind.Number:
                                value.Add(item.GetUInt64());
                                break;
                        }
                    }

                    await Config.UpdateImage(image, new string[] { "Reactions", reaction }, new JArray(value));

                    return value.ToArray();
                }
            }

            return NotFound();
        }

        [HttpDelete]
        [Route("{id}/reactions/{reaction}/{key}")]
        public async Task<ActionResult<object[]>> DeleteReactions([FromRoute(Name = "group")] string groupName, [FromRoute] string id, [FromRoute] string reaction, [FromRoute] string key)
        {
            if (Config.TryGetGroup(groupName, out Group group))
            {
                if (group.TryGetImage(id, out Image image))
                {
                    if (!image.Reactions.ContainsKey(reaction))
                    {
                        return NotFound();
                    }

                    List<object> reactions = image.Reactions[reaction].ToList();

                    object value = key;

                    if (!reactions.Contains(value))
                    {
                        if (ulong.TryParse(key, out ulong number))
                        {
                            if (reactions.Contains(number))
                            {
                                value = number;
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                    }

                    reactions.Remove(value);

                    await Config.UpdateImage(image, new string[] { "Reactions", reaction }, new JArray(reactions));

                    return reactions.ToArray();
                }
            }

            return NotFound();
        }
    }
}
