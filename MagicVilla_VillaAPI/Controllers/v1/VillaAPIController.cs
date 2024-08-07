﻿using AutoMapper;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.Dto;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

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
        public async Task<IActionResult> GetVillas([FromQuery(Name = "FilterOccupancy")] int? occupancy, [FromQuery] string? search,
            int pageSize = 0, int pageNumber = 1
            )
        {
            try
            {
                _logger.LogInformation("Getting all villas");
                List<Villa> villaList = new List<Villa>();
                if (occupancy > 0)
                {
                    villaList = await _villaRepository.GetAllAsync(u => u.Occupancy == occupancy, pageSize: pageSize,
                        pageNumber: pageNumber);
                }
                else
                {
                    villaList = await _villaRepository.GetAllAsync(pageSize: pageSize,
                        pageNumber: pageNumber);
                }
                if (!string.IsNullOrEmpty(search))
                {
                    villaList = villaList.Where(u => u.Amenity.ToLower().Contains(search)).ToList();
                }
                Pagination pagination = new Pagination { PageNumber = pageNumber, PageSize=pageSize };
                Response.Headers.Add("X-Pagination",JsonSerializer.Serialize(pagination));
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
        [Authorize(Roles ="admin")]
        public async Task<IActionResult> CreateVilla([FromForm] VillaCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    return BadRequest(createDto);
                }

                Villa villa = _mapper.Map<Villa>(createDto);
                await _villaRepository.CreateAsync(villa);

                if (createDto.Image != null) 
                {
                    string fileName = villa.Id + Path.GetExtension(createDto.Image.FileName);
                    string filePath = @"wwwroot\ProductImage\"+ fileName;

                    var directoryLocation= Path.Combine(Directory.GetCurrentDirectory(), filePath);

                    FileInfo file = new FileInfo(directoryLocation);

                    if (file.Exists) 
                    {
                        file.Delete();
                    }

                    using (var fileStream = new FileStream(directoryLocation, FileMode.Create))
                    {
                        createDto.Image.CopyTo(fileStream);
                    }

                    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                    villa.ImageUrl = baseUrl + "/ProductImage/" + fileName;
                    villa.ImageLocalPath = filePath;

                }
                else
                {
                    villa.ImageUrl = "https://placehold.co/600x400";
                }



                await _villaRepository.UpdateAsync(villa);
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
        [Authorize(Roles = "admin")]
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

                if (!string.IsNullOrEmpty(villa.ImageLocalPath))
                {
                    var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), villa.ImageLocalPath);
                    FileInfo file = new FileInfo(oldFilePathDirectory);

                    if (file.Exists)
                    {
                        file.Delete();
                    }
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

        [Authorize(Roles = "admin")]
        [HttpPut("{id:int}", Name = "UpdateVilla")]
        public async Task<IActionResult> UpdateVilla(int id, [FromForm] VillaUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null || id != updateDto.Id)
                {
                    return BadRequest();
                }

                Villa model = _mapper.Map<Villa>(updateDto);

                if (updateDto.Image != null)
                {
                    if (!string.IsNullOrEmpty(model.ImageLocalPath))
                    {
                        var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), model.ImageLocalPath);
                        FileInfo file = new FileInfo(oldFilePathDirectory);

                        if (file.Exists)
                        {
                            file.Delete();
                        }
                    }



                    string fileName = updateDto.Id + Path.GetExtension(updateDto.Image.FileName);
                    string filePath = @"wwwroot\ProductImage\" + fileName;

                    var directoryLocation = Path.Combine(Directory.GetCurrentDirectory(), filePath);

                    

                    using (var fileStream = new FileStream(directoryLocation, FileMode.Create))
                    {
                        updateDto.Image.CopyTo(fileStream);
                    }

                    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                    model.ImageUrl = baseUrl + "/ProductImage/" + fileName;
                    model.ImageLocalPath = filePath;
                }
                else
                {
                    model.ImageUrl = "https://placehold.co/600x400";
                }

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
