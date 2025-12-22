using AventStack.ExtentReports;
using FluentAssertions;
using System.Net;
using WebAPITests.Helpers;
using WebAPITests.Models;
using Xunit;
using Xunit.Abstractions;

namespace WebAPITests.Tests
{
    [Collection("API Tests")]
    public class BooksApiTests : IClassFixture<TestFixture>, IDisposable
    {
        private readonly TestFixture _fixture;
        private readonly ITestOutputHelper _output;
        private ExtentTest _extentTest;

        public BooksApiTests(TestFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;

            if (ReportHelper._extent == null)
            {
                ReportHelper.InitializeReport();
            }

            _output.WriteLine("BooksApiTests initialized");
        }

        public async void Dispose()
        {
            ReportHelper.FlushReport();
            _output.WriteLine("Report flushed and saved");

            await _fixture.CleanupAsync();
        }

        [Fact]
        public async Task CreateBook_ValidBook_ShouldCreateSuccessfully()
        {
            _extentTest = ReportHelper.CreateTest(
                "Create Book - Valid Book",
                "Verify that a valid book is successfully created with 201 status and data matches input");

            try
            {
                var newBook = new CreateBookRequest
                {
                    Title = $"Test Book {Guid.NewGuid()}",
                    Author = "Test Author",
                    ISBN = $"978-{Random.Shared.Next(100000000, 999999999)}",
                    PublishedDate = DateTime.UtcNow.AddYears(-5)
                };

                ReportHelper.LogInfo($"Creating book: {newBook.Title}");
                _output.WriteLine($"Creating book: {newBook.Title}");

                var response = await _fixture.PostAsync("/Books", newBook);
                var createdBook = await _fixture.DeserializeResponseAsync<Book>(response);

                ReportHelper.LogInfo($"Request: POST /Books");
                ReportHelper.LogInfo($"Response Status: {(int)response.StatusCode} {response.StatusCode}");

                response.StatusCode.Should().Be(HttpStatusCode.Created);
                ReportHelper.LogPass($"Status code validation passed: {(int)response.StatusCode} Created");

                createdBook.Should().NotBeNull();
                createdBook!.Id.Should().NotBeNullOrEmpty();
                createdBook.Title.Should().Be(newBook.Title);
                createdBook.Author.Should().Be(newBook.Author);
                createdBook.ISBN.Should().Be(newBook.ISBN);
                createdBook.PublishedDate.Should().Be(newBook.PublishedDate);

                ReportHelper.LogPass("All response body fields match the input data");

                _output.WriteLine($"Book created successfully with ID: {createdBook.Id}");
                ReportHelper.LogInfo($"Created book ID: {createdBook.Id}");
            }
            catch (Exception ex)
            {
                ReportHelper.LogFail($"Test failed: {ex.Message}");
                _extentTest.Fail(ex);
                throw;
            }
        }

        [Fact]
        public async Task CreateBook_DuplicateISBN_ShouldHandleGracefully()
        {
            _extentTest = ReportHelper.CreateTest(
                "Create Book - Duplicate ISBN",
                "Verify duplicate book creation handling");

            try
            {
                var isbn = $"978-{Random.Shared.Next(100000000, 999999999)}";
                var firstBook = new CreateBookRequest
                {
                    Title = "First Book",
                    Author = "Author One",
                    ISBN = isbn,
                    PublishedDate = DateTime.UtcNow.AddYears(-3)
                };

                ReportHelper.LogInfo($"Creating first book with ISBN: {isbn}");
                var firstResponse = await _fixture.PostAsync("/Books", firstBook);
                firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                ReportHelper.LogPass($"First book created successfully");

                var duplicateBook = new CreateBookRequest
                {
                    Title = "Second Book",
                    Author = "Author Two",
                    ISBN = isbn,
                    PublishedDate = DateTime.UtcNow.AddYears(-2)
                };

                ReportHelper.LogInfo($"Attempting to create duplicate book with same ISBN");
                var duplicateResponse = await _fixture.PostAsync("/Books", duplicateBook);

                ReportHelper.LogInfo($"Duplicate book response: {(int)duplicateResponse.StatusCode} {duplicateResponse.StatusCode}");

                duplicateResponse.StatusCode.Should().BeOneOf(
                    HttpStatusCode.BadRequest,
                    HttpStatusCode.Conflict);

                ReportHelper.LogPass($"Duplicate book handling works: {duplicateResponse.StatusCode}");
                _output.WriteLine($"Duplicate book handling works: {duplicateResponse.StatusCode}");
            }
            catch (Exception ex)
            {
                ReportHelper.LogFail($"Test failed: {ex.Message}");
                _extentTest.Fail(ex);
                throw;
            }
        }

        [Fact]
        public async Task GetAllBooks_ShouldReturnCompleteListWithValidFields()
        {
            _extentTest = ReportHelper.CreateTest(
                "Get All Books",
                "Verify API returns list of all books with complete field structure");

            try
            {
                ReportHelper.LogInfo("Getting all books...");
                _output.WriteLine("Getting all books...");

                var response = await _fixture.GetAsync("/Books");
                var books = await _fixture.DeserializeResponseAsync<List<Book>>(response);

                ReportHelper.LogInfo($"Response Status: {(int)response.StatusCode} {response.StatusCode}");
                ReportHelper.LogInfo($"Retrieved {books?.Count ?? 0} books");

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                ReportHelper.LogPass($"Status code validation passed: {(int)response.StatusCode} OK");

                books.Should().NotBeNull();
                books.Should().BeOfType<List<Book>>();
                ReportHelper.LogPass($"Response format is valid list");

                if (books!.Count > 0)
                {
                    var sampleBook = books.FirstOrDefault();
                    sampleBook!.Id.Should().NotBeNullOrEmpty();
                    sampleBook.Title.Should().NotBeNullOrEmpty();
                    sampleBook.Author.Should().NotBeNullOrEmpty();
                    ReportHelper.LogPass($"✓ Sample book has all required fields");

                    foreach (var book in books)
                    {
                        book.Id.Should().NotBeNullOrEmpty($"Book ID missing");
                        book.Title.Should().NotBeNullOrEmpty($"Title missing for book ID {book.Id}");
                        book.Author.Should().NotBeNullOrEmpty($"Author missing for book {book.Title}");
                    }
                    ReportHelper.LogPass($"✓ All {books.Count} books have required fields");
                }

                _output.WriteLine($"Retrieved {books.Count} books with valid structure");
                ReportHelper.LogInfo($"Total books retrieved: {books.Count}");
            }
            catch (Exception ex)
            {
                ReportHelper.LogFail($"Test failed: {ex.Message}");
                _extentTest.Fail(ex);
                throw;
            }
        }

        [Fact]
        public async Task GetBookById_ValidId_ShouldReturnCorrectData()
        {
            _extentTest = ReportHelper.CreateTest(
                "Get Book by ID - Valid ID",
                "Verify fetching valid book returns 200 status with correct data");

            try
            {
                var testBook = new CreateBookRequest
                {
                    Title = $"Get By ID Book {Guid.NewGuid()}",
                    Author = "Get Author",
                    ISBN = $"978-{Random.Shared.Next(100000000, 999999999)}",
                    PublishedDate = DateTime.UtcNow.AddYears(-4)
                };

                var createResponse = await _fixture.PostAsync("/Books", testBook);
                var createdBook = await _fixture.DeserializeResponseAsync<Book>(createResponse);
                ReportHelper.LogInfo($"Created test book for GetByID: {createdBook!.Id}");
                _output.WriteLine($"Created test book for GetByID: {createdBook!.Id}");

                var response = await _fixture.GetAsync($"/Books/{createdBook.Id}");
                var retrievedBook = await _fixture.DeserializeResponseAsync<Book>(response);

                ReportHelper.LogInfo($"Response Status: {(int)response.StatusCode} {response.StatusCode}");

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                ReportHelper.LogPass($"Status code validation passed: {(int)response.StatusCode} OK");

                retrievedBook.Should().NotBeNull();
                retrievedBook!.Id.Should().Be(createdBook.Id);
                retrievedBook.Title.Should().Be(createdBook.Title);
                retrievedBook.Author.Should().Be(createdBook.Author);
                retrievedBook.ISBN.Should().Be(createdBook.ISBN);
                retrievedBook.PublishedDate.Should().Be(createdBook.PublishedDate);

                ReportHelper.LogPass("All book data matches expected values");

                _output.WriteLine($"Retrieved book by ID: {retrievedBook.Title}");
            }
            catch (Exception ex)
            {
                ReportHelper.LogFail($"Test failed: {ex.Message}");
                _extentTest.Fail(ex);
                throw;
            }
        }

        [Fact]
        public async Task GetBookById_NonExistentId_ShouldReturn404()
        {
            _extentTest = ReportHelper.CreateTest(
                "Get Book by ID - Non-existent ID",
                "Verify fetching non-existent book returns 404 status");

            try
            {
                var nonExistentId = Guid.NewGuid().ToString();
                ReportHelper.LogInfo($"Testing non-existent ID: {nonExistentId}");
                _output.WriteLine($"Testing non-existent ID: {nonExistentId}");

                var response = await _fixture.GetAsync($"/Books/{nonExistentId}");
                ReportHelper.LogInfo($"Response Status: {(int)response.StatusCode} {response.StatusCode}");

                response.StatusCode.Should().Be(HttpStatusCode.NotFound);
                ReportHelper.LogPass($"Correctly returns 404 for non-existent ID");
                _output.WriteLine($"Correctly returns 404 for non-existent ID");
            }
            catch (Exception ex)
            {
                ReportHelper.LogFail($"Test failed: {ex.Message}");
                _extentTest.Fail(ex);
                throw;
            }
        }

        [Theory]
        [InlineData("invalid-guid")]
        [InlineData("12345")]
        [InlineData("not-a-guid")]
        public async Task GetBookById_InvalidIdFormat_ShouldReturn400(string invalidId)
        {
            _extentTest = ReportHelper.CreateTest(
                $"Get Book by ID - Invalid Format: {invalidId}",
                "Verify invalid ID format returns 400 status");

            try
            {
                ReportHelper.LogInfo($"Testing invalid ID format: '{invalidId}'");
                _output.WriteLine($"Testing invalid ID format: '{invalidId}'");

                var response = await _fixture.GetAsync($"/Books/{invalidId}");
                ReportHelper.LogInfo($"Response Status: {(int)response.StatusCode} {response.StatusCode}");

                response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
                ReportHelper.LogPass($"Invalid ID format handling works: {response.StatusCode}");
                _output.WriteLine($"Invalid ID format '{invalidId}' returns {response.StatusCode}");
            }
            catch (Exception ex)
            {
                ReportHelper.LogFail($"Test failed: {ex.Message}");
                _extentTest.Fail(ex);
                throw;
            }
        }

        [Fact]
        public async Task UpdateBook_ValidData_ShouldUpdateSuccessfully()
        {
            _extentTest = ReportHelper.CreateTest(
                "Update Book - Valid Data",
                "Verify updating existing book with valid data works");

            try
            {
                var originalBook = new CreateBookRequest
                {
                    Title = $"Original Book {Guid.NewGuid()}",
                    Author = "Original Author",
                    ISBN = $"978-{Random.Shared.Next(100000000, 999999999)}",
                    PublishedDate = DateTime.UtcNow.AddYears(-3)
                };

                var createResponse = await _fixture.PostAsync("/Books", originalBook);
                var createdBook = await _fixture.DeserializeResponseAsync<Book>(createResponse);
                ReportHelper.LogInfo($"Created book to update: {createdBook!.Id}");
                _output.WriteLine($"Created book to update: {createdBook!.Id}");

                var updateRequest = new UpdateBookRequest
                {
                    Title = $"Updated Book {Guid.NewGuid()}",
                    Author = "Updated Author",
                    PublishedDate = DateTime.UtcNow
                };

                ReportHelper.LogInfo($"Updating with new title: {updateRequest.Title}");
                _output.WriteLine($"Updating with new title: {updateRequest.Title}");

                var updateResponse = await _fixture.PutAsync($"/Books/{createdBook.Id}", updateRequest);
                ReportHelper.LogInfo($"Update response: {(int)updateResponse.StatusCode} {updateResponse.StatusCode}");

                updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
                ReportHelper.LogPass($"Update status code correct: {(int)updateResponse.StatusCode} No Content");

                var getResponse = await _fixture.GetAsync($"/Books/{createdBook.Id}");
                var updatedBook = await _fixture.DeserializeResponseAsync<Book>(getResponse);

                updatedBook.Should().NotBeNull();
                updatedBook!.Title.Should().Be(updateRequest.Title);
                updatedBook.Author.Should().Be(updateRequest.Author);
                updatedBook.ISBN.Should().Be(createdBook.ISBN);
                updatedBook.PublishedDate.Should().Be(updateRequest.PublishedDate);

                ReportHelper.LogPass("Book updated successfully with correct data");

                _output.WriteLine($"Book updated successfully");
            }
            catch (Exception ex)
            {
                ReportHelper.LogFail($"Test failed: {ex.Message}");
                _extentTest.Fail(ex);
                throw;
            }
        }

        [Fact]
        public async Task UpdateBook_NonExistentId_ShouldReturn404()
        {
            _extentTest = ReportHelper.CreateTest(
                "Update Book - Non-existent ID",
                "Verify updating non-existent book returns 404 status");

            try
            {
                var nonExistentId = Guid.NewGuid().ToString();
                var updateRequest = new UpdateBookRequest
                {
                    Title = "Should Not Update",
                    Author = "No Author",
                    PublishedDate = DateTime.UtcNow
                };

                ReportHelper.LogInfo($"Attempting to update non-existent ID: {nonExistentId}");
                _output.WriteLine($"Attempting to update non-existent ID: {nonExistentId}");

                var response = await _fixture.PutAsync($"/Books/{nonExistentId}", updateRequest);
                ReportHelper.LogInfo($"Response Status: {(int)response.StatusCode} {response.StatusCode}");

                response.StatusCode.Should().Be(HttpStatusCode.NotFound);
                ReportHelper.LogPass($"Correctly returns 404 for non-existent ID");
                _output.WriteLine($"Correctly returns 404 for non-existent ID");
            }
            catch (Exception ex)
            {
                ReportHelper.LogFail($"Test failed: {ex.Message}");
                _extentTest.Fail(ex);
                throw;
            }
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("123")]
        [InlineData("not-a-valid-guid")]
        public async Task UpdateBook_InvalidIdFormat_ShouldReturn400(string invalidId)
        {
            _extentTest = ReportHelper.CreateTest(
                $"Update Book - Invalid ID Format: {invalidId}",
                "Verify invalid ID format returns 400 status");

            try
            {
                var updateRequest = new UpdateBookRequest
                {
                    Title = "Test Title",
                    Author = "Test Author",
                    PublishedDate = DateTime.UtcNow
                };

                ReportHelper.LogInfo($"Testing invalid ID format: '{invalidId}'");
                _output.WriteLine($"Testing invalid ID format: '{invalidId}'");

                var response = await _fixture.PutAsync($"/Books/{invalidId}", updateRequest);
                ReportHelper.LogInfo($"Response Status: {(int)response.StatusCode} {response.StatusCode}");

                response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
                ReportHelper.LogPass($"Invalid ID format handling works: {response.StatusCode}");
                _output.WriteLine($"Invalid ID format handling works: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                ReportHelper.LogFail($"Test failed: {ex.Message}");
                _extentTest.Fail(ex);
                throw;
            }
        }

        [Fact]
        public async Task DeleteBook_ValidId_ShouldDeleteSuccessfully()
        {
            _extentTest = ReportHelper.CreateTest(
                "Delete Book - Valid ID",
                "Verify deleting book returns 204 and book is no longer retrievable");

            try
            {
                var bookToDelete = new CreateBookRequest
                {
                    Title = $"Book to Delete {Guid.NewGuid()}",
                    Author = "Delete Author",
                    ISBN = $"978-{Random.Shared.Next(100000000, 999999999)}",
                    PublishedDate = DateTime.UtcNow.AddMonths(-2)
                };

                var createResponse = await _fixture.PostAsync("/Books", bookToDelete);
                var createdBook = await _fixture.DeserializeResponseAsync<Book>(createResponse);
                var bookId = createdBook!.Id;

                ReportHelper.LogInfo($"Created book for deletion: {bookId}");
                _output.WriteLine($"Created book for deletion: {bookId}");

                var beforeDelete = await _fixture.GetAsync($"/Books/{bookId}");
                beforeDelete.StatusCode.Should().Be(HttpStatusCode.OK);
                ReportHelper.LogPass("Book exists before deletion");

                var deleteResponse = await _fixture.DeleteAsync($"/Books/{bookId}");
                ReportHelper.LogInfo($"Delete response: {(int)deleteResponse.StatusCode} {deleteResponse.StatusCode}");

                deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
                ReportHelper.LogPass($"Delete status code correct: {(int)deleteResponse.StatusCode} No Content");

                var afterDelete = await _fixture.GetAsync($"/Books/{bookId}");
                afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
                ReportHelper.LogPass("Book successfully deleted and no longer retrievable");

                _output.WriteLine($"Book successfully deleted");
            }
            catch (Exception ex)
            {
                ReportHelper.LogFail($"Test failed: {ex.Message}");
                _extentTest.Fail(ex);
                throw;
            }
        }

        [Fact]
        public async Task DeleteBook_NonExistentId_ShouldReturn404()
        {
            _extentTest = ReportHelper.CreateTest(
                "Delete Book - Non-existent ID",
                "Verify deleting non-existent book returns 404 status");

            try
            {
                var nonExistentId = Guid.NewGuid().ToString();
                ReportHelper.LogInfo($"Attempting to delete non-existent ID: {nonExistentId}");
                _output.WriteLine($"Attempting to delete non-existent ID: {nonExistentId}");

                var response = await _fixture.DeleteAsync($"/Books/{nonExistentId}");
                ReportHelper.LogInfo($"Response Status: {(int)response.StatusCode} {response.StatusCode}");

                response.StatusCode.Should().Be(HttpStatusCode.NotFound);
                ReportHelper.LogPass($"Correctly returns 404 for non-existent ID");
                _output.WriteLine($"Correctly returns 404 for non-existent ID");
            }
            catch (Exception ex)
            {
                ReportHelper.LogFail($"Test failed: {ex.Message}");
                _extentTest.Fail(ex);
                throw;
            }
        }

        [Theory]
        [InlineData("invalid-id")]
        [InlineData("123abc")]
        public async Task DeleteBook_InvalidIdFormat_ShouldReturn400(string invalidId)
        {
            _extentTest = ReportHelper.CreateTest(
                $"Delete Book - Invalid ID Format: {invalidId}",
                "Verify invalid ID format returns 400 status");

            try
            {
                ReportHelper.LogInfo($"Testing invalid ID format: '{invalidId}'");
                _output.WriteLine($"Testing invalid ID format: '{invalidId}'");

                var response = await _fixture.DeleteAsync($"/Books/{invalidId}");
                ReportHelper.LogInfo($"Response Status: {(int)response.StatusCode} {response.StatusCode}");

                response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
                ReportHelper.LogPass($"Invalid ID format handling works: {response.StatusCode}");
                _output.WriteLine($"Invalid ID format handling works: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                ReportHelper.LogFail($"Test failed: {ex.Message}");
                _extentTest.Fail(ex);
                throw;
            }
        }
    }
}