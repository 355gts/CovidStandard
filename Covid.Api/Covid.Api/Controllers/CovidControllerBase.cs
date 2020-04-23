using Covid.Common.Mapper;
using Covid.Repository.Facades;
using System;
using System.Web.Http;

namespace Covid.Api.Controllers
{
    public class CovidControllerBase : ApiController
    {
        protected readonly ICovidRepositoryFacade _repositoryFacade;
        protected readonly IMapper _mapper;

        public CovidControllerBase(
            ICovidRepositoryFacade repositoryFacade,
            IMapper mapper)
        {
            _repositoryFacade = repositoryFacade ?? throw new ArgumentNullException(nameof(repositoryFacade));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
    }
}
