﻿using AutoMapper;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.Dto;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MagicVilla_VillaAPI.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class VillaAPIController : ControllerBase
    {
        public readonly ILogger<VillaAPIController> _logger;
        private readonly IVillaRepository _villaRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        public VillaAPIController(ILogger<VillaAPIController> logger, IMapper mapper, IVillaRepository villaRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _villaRepository = villaRepository;
            this._response = new();
        }


        [HttpGet]
        // [MapToApiVersion("2.0")]
        //[ResponseCache(Duration =30)]
        public async Task<IActionResult> GetVillas([FromQuery(Name ="FilterOccupancy")]int? occupancy)
        {
            try
            {
                _logger.LogInformation("Getting all villas");
                List<Villa> villaList = new List<Villa>();
                if (occupancy > 0)
                {
                     villaList = await _villaRepository.GetAllAsync(u=>u.Occupancy ==occupancy);
                }
                else
                {
                     villaList = await _villaRepository.GetAllAsync();
                }
               
                _response.Result = _mapper.Map<List<VillaDto>>(villaList);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting villas");
                var errorResponse = new
                {
                    IsSuccess = false,
                    ErrorMessages = new List<string>() { ex.Message }
                };
                return BadRequest(errorResponse);
            }

        }

        [HttpGet("{id:int}")]
      //  [ResponseCache(Location =ResponseCacheLocation.None, NoStore =true)]
        public async Task<IActionResult> GetVilla(int id)
        {
            try
            {
                if (id == 0)
                {
                    _logger.LogError($"problem with id {id}");
                    return BadRequest();
                }
                var villa = await _villaRepository.GetAsync(u => u.Id == id);
                if (villa == null)
                {
                    return NotFound();
                }
                _response.Result = _mapper.Map<VillaDto>(villa);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting villas");
                var errorResponse = new
                {
                    IsSuccess = false,
                    ErrorMessages = new List<string>() { ex.Message }
                };
                return BadRequest(errorResponse);
            }

        }

        [HttpPost]
      //  [Authorize(Roles ="admin")]
        public async Task<IActionResult> CreateVilla([FromBody] VillaCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    return BadRequest(createDto);
                }

                Villa villa = _mapper.Map<Villa>(createDto);
                await _villaRepository.CreateAsync(villa);
                _response.Result = _mapper.Map<VillaDto>(villa);
                _response.StatusCode = HttpStatusCode.Created;
                return CreatedAtRoute(new { id = villa.Id }, _response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting villas");
                var errorResponse = new
                {
                    IsSuccess = false,
                    ErrorMessages = new List<string>() { ex.Message }
                };
                return BadRequest(errorResponse);
            }

        }

        [HttpDelete("{id:int}", Name = "DeleteVilla")]
        //[Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteVilla(int id)
        {
            try
            {
                if (id == 0)
                {
                    return BadRequest();
                }
                var villa = await _villaRepository.GetAsync(u => u.Id == id);
                if (villa == null)
                {
                    return NotFound();
                }
                await _villaRepository.RemoveAsync(villa);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting villas");
                var errorResponse = new
                {
                    IsSuccess = false,
                    ErrorMessages = new List<string>() { ex.Message }
                };
                return BadRequest(errorResponse);
            }
        }

        [HttpPut("{id:int}", Name = "UpdateVilla")]
        public async Task<IActionResult> UpdateVilla(int id, [FromBody] VillaUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null || id != updateDto.Id)
                {
                    return BadRequest();
                }

                Villa model = _mapper.Map<Villa>(updateDto);

                await _villaRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting villas");
                var errorResponse = new
                {
                    IsSuccess = false,
                    ErrorMessages = new List<string>() { ex.Message }
                };
                return BadRequest(errorResponse);
            }

        }

        [HttpPatch("{id:int}", Name = "UpdatePartialVilla")]
        public async Task<IActionResult> UpdatePartialVilla(int id, JsonPatchDocument<VillaDto> patchDto)
        {

            try
            {
                if (patchDto == null || id == 0)
                {
                    return BadRequest();
                }
                var villa = await _villaRepository.GetAsync(u => u.Id == id, tracked: false);

                VillaDto villaDto = _mapper.Map<VillaDto>(villa);



                if (villa == null)
                {
                    return BadRequest();
                }
                patchDto.ApplyTo(villaDto, ModelState);

                Villa model = _mapper.Map<Villa>(villaDto);


                await _villaRepository.UpdateAsync(model);
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting villas");
                var errorResponse = new
                {
                    IsSuccess = false,
                    ErrorMessages = new List<string>() { ex.Message }
                };
                return BadRequest(errorResponse);
            }

        }
    }
}
