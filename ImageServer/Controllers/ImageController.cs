using ImageServer.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace ImageServer.Controllers
{
    [Route("api/groups/{group}")]
    [ApiController]
    public partial class ImageController : ControllerBase
    {
        /// <summary>
        /// Random number generator for "random" route
        /// </summary>
        static Random random = new Random();

        [HttpGet]
        public ActionResult<Image[]> List([FromRoute(Name = "group")] string name)
        {
            if (Config.TryGetGroup(name, out Group group))
            {
                return group.Images.Values.ToArray();
            }

            return NotFound();
        }

        /// <summary>
        /// Get a random image from the specified group
        /// </summary>
        /// <param name="groupName">Name of Group of images to query</param>
        /// <returns></returns>
        [HttpGet]
        [Route("random")]
        public ActionResult<Image> Random([FromRoute(Name = "group")] string groupName)
        {
            if (Config.TryGetGroup(groupName, out Group group))
            {
                lock (group.Images)
                {
                    var images = group.Images.Values.ToArray();

                    if (images.Length == 0)
                    {
                        return NotFound();
                    }

                    var index = random.Next(0, images.Length);
                    return images[index];
                }
            }

            return NotFound();
        }

        /// <summary>
        /// Get a specific image from a Group by it's Id
        /// </summary>
        /// <param name="groupName">The name of the Group to look in</param>
        /// <param name="id">The Id of the item to fetch, typically the SHA1 of the file + ext</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        public ActionResult<Image> Get([FromRoute(Name = "group")] string groupName, [FromRoute] string id)
        {
            if (Config.TryGetGroup(groupName, out Group group))
            {
                if (group.TryGetImage(id, out Image image))
                {
                    return image;
                }
            }

            return NotFound();
        }

        // TODO: DELETE action to remove images from Group
        // TODO: PUT action to add a new image to a Group
    }
}
