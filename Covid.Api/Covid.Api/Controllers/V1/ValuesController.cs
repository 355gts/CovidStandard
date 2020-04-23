using Covid.Common.Mapper;
using Covid.Repository.Facades;
using System.Web.Http;
using System.Web.Http.Description;

namespace Covid.Api.Controllers.V1
{
    //[Route("api/{version:apiVersion}/[controller]")]
    //[Route("api/[controller]")]
    [RoutePrefix("v1")]
    public class ValuesController : CovidControllerBase
    {
        public ValuesController(
            ICovidRepositoryFacade repositoryFacade,
            IMapper mapper)
            : base(repositoryFacade, mapper)
        {

        }

        // GET api/values
        [HttpGet]
        [ResponseType(typeof(string[]))]
        public IHttpActionResult Get()
        {
            return Ok(new string[] { "v1.1", "v1.2" });
        }

        // GET api/values/5
        [HttpGet]
        [Route("{id}")]
        [ResponseType(typeof(string))]
        public IHttpActionResult Get(int id)
        {
            return Ok("value");
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut]
        [Route("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete]
        [Route("{id}")]
        public void Delete(int id)
        {
        }
    }
}
