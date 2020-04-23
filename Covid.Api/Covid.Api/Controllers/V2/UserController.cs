using Covid.Common.Mapper;
using Covid.Repository.Facades;
using Covid.Web.Model.Users;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Dom = Covid.Web.Model.Users;
using Repo = Covid.Repository.Model.Users;

namespace Covid.Api.Controllers.V2
{
    /// <summary>
    /// Instantiates an instance of the User controller
    /// </summary>
    //[Route("api/{version:apiVersion}/users")]
    //[Route("api/users")]
    [RoutePrefix("v2")]
    public class UserController : CovidControllerBase
    {
        public UserController(
            ICovidRepositoryFacade repositoryFacade,
            IMapper mapper)
            : base(repositoryFacade, mapper)
        {

        }

        [HttpGet]
        [ResponseType(typeof(IEnumerable<User>))]
        public async Task<IHttpActionResult> GetUsersAsync()
        {
            var repoUsers = await _repositoryFacade.Users.GetUsersAsync();

            var users = _mapper.MapEnumerable<Repo.User, Dom.User>(repoUsers);

            return Ok(users);
        }

        [HttpGet]
        [ResponseType(typeof(User))]
        [Route("{id:int}", Name = nameof(GetUserByIdAsync))]
        public async Task<IHttpActionResult> GetUserByIdAsync(int id)
        {
            var repoUser = await _repositoryFacade.Users.GetUserByIdAsync(id);
            if (repoUser == null)
                return NotFound();

            return Ok(_mapper.Map<Repo.User, Dom.User>(repoUser));
        }

        [HttpPost]
        [Route("create")]
        public async Task<IHttpActionResult> CreateUserAsync([FromBody] Dom.CreateUser user)
        {
            var repoUser = _mapper.Map<Dom.CreateUser, Repo.User>(user);

            var result = await _repositoryFacade.Users.CreateUserAsync(repoUser);
            if (!result.Success)
                return InternalServerError();

            return CreatedAtRoute(nameof(GetUserByIdAsync), new { id = result.Value }, result.Value);
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IHttpActionResult> UpdateUserAsync(int id, [FromBody] Dom.User user)
        {
            var repoUser = await _repositoryFacade.Users.GetUserByIdAsync(id);
            if (repoUser == null)
                return NotFound();

            _mapper.Map<Dom.User, Repo.User>(user, repoUser);

            var success = await _repositoryFacade.Users.UpdateUserAsync(id, repoUser);
            if (!success)
                return InternalServerError();

            return Ok();
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IHttpActionResult> DeleteUserAsync(int id)
        {
            var repoUser = await _repositoryFacade.Users.GetUserByIdAsync(id);
            if (repoUser == null)
                return NotFound();

            var success = await _repositoryFacade.Users.DeleteUserAsync(id);
            if (!success)
                return InternalServerError();

            return Ok();
        }
    }
}
