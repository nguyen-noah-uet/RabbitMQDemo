using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Plain.RabbitMQ;
using RabbitMQ.Client;
using Shared.DTOs;
using Shared.Messages;
using Shared.Models;
using Shared.Repositories;

namespace Api1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly IMessagePublisherService _publisherService;
        private readonly ILogger<BookController> _logger;

        public BookController(IBookRepository bookRepository, IMessagePublisherService publisherService, ILogger<BookController> logger)
        {
            _bookRepository = bookRepository;
            _publisherService = publisherService;
            _logger = logger;
        }

        // GET: api/book
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
        {
            _logger.LogInformation("Getting all books");
            return Ok(await _bookRepository.GetAllBooksAsync());
        }

        [HttpGet]
        [Route("{id:int}")]
        public async Task<ActionResult<Book>> GetBookById(int id)
        {
            _logger.LogInformation($"Getting book with id {id}");
            var book = await _bookRepository.GetBookByIdAsync(id);
            if (book == null)
            {
                _logger.LogWarning($"Book with id {id} not found");
                return NotFound();
            }
            _logger.LogInformation($"Returning book with id {id}");
            return Ok(book);
        }
        // POST: api/book
        [HttpPost]
        public async Task<ActionResult<Book>> AddBook(Book book)
        {
            try
            {
                _logger.LogInformation($"Adding book with id {book.Id}");
                var addedBook = await _bookRepository.AddBookAsync(book);
                _logger.LogInformation($"Server 1 added book with id {book.Id}");
                _logger.LogInformation($"Publishing added book with id {book.Id}");
                S1UpdatedMessage message = new S1UpdatedMessage
                {
                    BookForUpdate = addedBook,
                    ActionType = Shared.Enums.ActionType.Add
                };

                //_publisher.Publish(JsonSerializer.Serialize(message), "order_created_routingkey", null);
                _publisherService.Publish(message, "s1.exchange", ExchangeType.Direct, "s1.queue", "s1.routekey");
                return CreatedAtAction(nameof(GetBookById), new { id = addedBook.Id }, addedBook);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return StatusCode(500);
            }
        }
        // PUT: api/book
        [HttpPut]
        [Route("{id:int}")]
        public async Task<ActionResult<Book>> UpdateBook(int id, BookForUpdateDto bookDto)
        {
            try
            {
                Book book = new Book()
                {
                    Id = id,
                    Title = bookDto.Title,
                    Price = bookDto.Price,
                };
                _logger.LogInformation($"Updating book with id {book.Id}");
                var updatedBook = await _bookRepository.UpdateBookAsync(book);
                _logger.LogInformation($"Server 1 updated book with id {book.Id}");
                _logger.LogInformation($"Publishing updated book with id {book.Id}");
                S1UpdatedMessage message = new S1UpdatedMessage
                {
                    BookForUpdate = updatedBook,
                    ActionType = Shared.Enums.ActionType.Update
                };

                _publisherService.Publish(message, "s1.exchange", ExchangeType.Direct, "s1.queue", "s1.routekey");

                return NoContent();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return StatusCode(500);
            }
        }
        // DELETE: api/book
        [HttpDelete]
        [Route("{id:int}")]
        public async Task<ActionResult<Book>> DeleteBook(int id)
        {
            try
            {
                _logger.LogInformation($"Deleting book with id {id}");
                var deletedBook = await _bookRepository.DeleteBookAsync(id);
                if (deletedBook == null)
                {
                    _logger.LogWarning($"Book with id {id} not found");
                    return NotFound();
                }

                _logger.LogInformation($"Server 1 deleted book with id {id}");
                _logger.LogInformation($"Publishing deleted book with id {id}");
                S1UpdatedMessage message = new S1UpdatedMessage
                {
                    BookForUpdate = deletedBook,
                    ActionType = Shared.Enums.ActionType.Delete
                };

                _publisherService.Publish(message, "s1.exchange",  ExchangeType.Direct, "s1.queue", "s1.routekey");

                return NoContent();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return StatusCode(500);
            }
        }
    }
}
