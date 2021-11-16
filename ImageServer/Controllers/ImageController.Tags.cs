using ImageServer.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace ImageServer.Controllers
{
    public partial class ImageController
    {
        [HttpPost]
        [Route("{id}/tags")]
        public async Task<ActionResult<string[]>> AddTags([FromRoute(Name = "group")] string groupName, [FromRoute] string id, [FromBody] string tag)
        {
            if (Config.TryGetGroup(groupName, out Group group))
            {
                if (group.TryGetImage(id, out Image image))
                {
                    var tags = image.Tags.ToList();
                    
                    tags.Add(tag);
                    
                    await Config.UpdateImage(image, "Tags", new JArray(tags));
                    
                    return tags.ToArray();
                }
            }

            return NotFound();
        }

        [HttpPut]
        [Route("{id}/tags")]
        public async Task<ActionResult<string[]>> ReplaceTags([FromRoute(Name = "group")] string groupName, [FromRoute] string id, [FromBody] string[] tags)
        {
            if (Config.TryGetGroup(groupName, out Group group))
            {
                if (group.TryGetImage(id, out Image image))
                {
                    await Config.UpdateImage(image, "Tags", new JArray(tags));
                    
                    return tags.ToArray();
                }
            }

            return NotFound();
        }

        [HttpDelete]
        [Route("{id}/tags/{tag}")]
        public async Task<ActionResult<string[]>> DeleteTags([FromRoute(Name = "group")] string groupName, [FromRoute] string id, [FromRoute] string tag)
        {
            if (Config.TryGetGroup(groupName, out Group group))
            {
                if (group.TryGetImage(id, out Image image))
                {
                    var tags = image.Tags.ToList();

                    if (!tags.Contains(tag))
                    {
                        return NotFound();
                    }

                    tags.Remove(tag);
                    
                    await Config.UpdateImage(image, "Tags", new JArray(tags));
                    
                    return tags.ToArray();
                }
            }

            return NotFound();
        }
    }
}