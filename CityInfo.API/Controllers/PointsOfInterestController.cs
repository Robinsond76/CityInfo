using AutoMapper;
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityInfo.API.Controllers
{
    [ApiController]
    [Route("api/cities/{cityId}/pointsofinterest")]
    public class PointsOfInterestController : ControllerBase
    {
        private readonly ILogger<PointsOfInterestController> _logger;
        private readonly IMailService _mailService;
        private readonly ICityInfoRepository _cityInfoRepository;
        private readonly IMapper _mapper;

        public PointsOfInterestController(ILogger<PointsOfInterestController> logger, 
            IMailService mailService, ICityInfoRepository cityInfoRepository, IMapper mapper)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _mailService = mailService ??
                throw new ArgumentNullException(nameof(mailService));
            _cityInfoRepository = cityInfoRepository ?? 
                throw new ArgumentNullException(nameof(cityInfoRepository)); 
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
            ;
        }

        [HttpGet]
        public IActionResult GetPointsOfInterest(int cityId)
        {
            //if city not found, return 404
            if (!_cityInfoRepository.CityExists(cityId))
                return NotFound();
            
            var pointsOfInterestForCity = _cityInfoRepository.GetPointsOfInterestForCity(cityId);
            return Ok(_mapper.Map<IEnumerable<PointOfInterestDto>>(pointsOfInterestForCity));
        }

        [HttpGet("{id}", Name = "GetPointOfInterest")]
        public IActionResult GetPointOfInterest( int cityId, int id)
        {
            //if city not found, return 404
            if (!_cityInfoRepository.CityExists(cityId))
                return NotFound();

            var pointOfInterest = _cityInfoRepository.GetPointOfInterestForCity(cityId, id);

            //if poi not found, return 404
            if (pointOfInterest == null)
                return NotFound();

            return Ok(_mapper.Map<PointOfInterestDto>(pointOfInterest));
        }

        [HttpPost]
        public IActionResult CreatePointOfInterest(int cityId, 
            [FromBody] PointOfInterestForCreationDto pointOfInterest)
        {
            if (pointOfInterest.Description == pointOfInterest.Name)
            {
                ModelState.AddModelError(
                    "Description",
                    "The provided description should be different from the name.");
            }

            //Validate Model
            if (!ModelState.IsValid)
                return BadRequest(ModelState);


            //if city not found, return 404
            if (!_cityInfoRepository.CityExists(cityId))
                return NotFound();

            var finalPointOfInterest = _mapper.Map<Entities.PointOfInterest>(pointOfInterest);

            _cityInfoRepository.AddPointOfInterestForCity(cityId, finalPointOfInterest);
           
            _cityInfoRepository.Save();

            var createdPointOfInterestToReturn = _mapper
                .Map<Models.PointOfInterestDto>(finalPointOfInterest);

            return CreatedAtRoute(
                "GetPointOfInterest",
                new {cityId, id = createdPointOfInterestToReturn.Id },
                createdPointOfInterestToReturn);
        }

        [HttpPut("{id}")]
        public IActionResult UpdatePointOfInterest(int cityId, int id,
            [FromBody] PointOfInterestForUpdateDto pointOfInterest)
        {
            if (pointOfInterest.Description == pointOfInterest.Name)
            {
                ModelState.AddModelError(
                    "Description",
                    "The provided description should be different from the name.");
            }

            //Validate Model
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //if city not found, return 404
            if (!_cityInfoRepository.CityExists(cityId))
                return NotFound();

            //Find Point of Interest
            var pointOfInterestEntity = _cityInfoRepository
                .GetPointOfInterestForCity(cityId, id);
            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            //New method from mapper. Here mapper will overwrite the data in the second parameter with data
            // from the first parameter. AutoMapper will override the values in the destination object with 
            // those in the source object. As the destination object is an entity tracked by our DBContext, 
            // it now has a modified state. 
            _mapper.Map(pointOfInterest, pointOfInterestEntity);

            //So, once we call Save, the changes will effectively be persisted to the database.
            _cityInfoRepository.Save();


            //EF Core tracks it's entities, but if we were to switch repositorys and use a tech that doesn't,
            //the code would break as it would need some kind of update functionality. As best practice, we implement
            //here an empty placeholder method.
            _cityInfoRepository.UpdatePointOfInterestForCity(cityId, pointOfInterestEntity);

            return NoContent(); //Request completed successfully, nothing to return.
        }

        [HttpPatch("{id}")]
        public IActionResult PartiallyUpdatePointOfInterest(int cityId, int id,
            [FromBody] JsonPatchDocument<PointOfInterestForUpdateDto> patchDoc)
        {
            //Find the city
            var city = CitiesDataStore.Current.Cities
                .FirstOrDefault(c => c.Id == cityId);
            if (city == null)
                return NotFound();

            //Find Point of Interest
            var pointOfInterestFromStore = city.PointsOfInterest
                .FirstOrDefault(p => p.Id == id);
            if (pointOfInterestFromStore == null)
                return NotFound();

            var pointOfInterestToPatch =
                new PointOfInterestForUpdateDto()
                {
                    Name = pointOfInterestFromStore.Name,
                    Description = pointOfInterestFromStore.Description
                };

            patchDoc.ApplyTo(pointOfInterestToPatch, ModelState);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //custom validation
            if (pointOfInterestToPatch.Description == pointOfInterestToPatch.Name)
            {
                ModelState.AddModelError(
                    "Description",
                    "The provided description should be different from the name.");
            }

            if (!TryValidateModel(pointOfInterestToPatch))
            {
                return BadRequest(ModelState);
            }

            pointOfInterestFromStore.Name = pointOfInterestToPatch.Name;
            pointOfInterestFromStore.Description = pointOfInterestToPatch.Description;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeletePointOfInterest(int cityId, int id)
        {
            //find city
            var city = CitiesDataStore.Current.Cities
                .FirstOrDefault(c => c.Id == cityId);
            if (city == null)
            {
                return NotFound();
            }

            //find POI
            var pointOfInterestFromStore = city.PointsOfInterest
                .FirstOrDefault(c => c.Id == id);
            if (pointOfInterestFromStore == null)
            {
                return NotFound();
            }

            city.PointsOfInterest.Remove(pointOfInterestFromStore);

            _mailService.Send("Point of interest deleted.",
                    $"Point of interest {pointOfInterestFromStore.Name} with id {pointOfInterestFromStore.Id} was deleted.");

            return NoContent();
        }
    }
}
