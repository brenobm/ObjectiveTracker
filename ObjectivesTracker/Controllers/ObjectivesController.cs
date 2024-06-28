using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using ObjectivesTracker.Models;

namespace ObjectsTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ObjectivesController(Container container) : ControllerBase
    {
        private readonly string _defaultPartitionKey = "default";

        [HttpGet]
        public async Task<IActionResult> GetAllObjectives()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var iterator = container.GetItemQueryIterator<Objective>(query);
            var objectives = new List<Objective>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                objectives.AddRange(response.Resource);
            }

            return Ok(objectives);
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewObjective(Objective newObjective)
        {
            // Input Validation (Example)
            if (string.IsNullOrWhiteSpace(newObjective.Name))
            {
                return BadRequest("Objective name is required.");
            }

            // Generate a unique ID (You can use a GUID or other strategy)
            newObjective.Id = Guid.NewGuid().ToString();
            newObjective.PartitionKey = _defaultPartitionKey;

            // Save to Cosmos DB
            try
            {
                var response = await container.CreateItemAsync(newObjective, new PartitionKey(_defaultPartitionKey));
                return CreatedAtAction(nameof(GetAllObjectives), new { id = newObjective.Id }, response.Resource);
            }
            catch (CosmosException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Message);
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveObjectivesForTheCurrentDay()
        {
            var today = DateOnly.FromDateTime(DateTime.Now); // Get current UTC date

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.frequency.type = 0 OR " +
                "(c.frequency.type = 1 AND ARRAY_CONTAINS(c.frequency.days, @dayOfWeek)) OR " +
                "(c.frequency.type = 2 AND ARRAY_CONTAINS(c.frequency.days, @dayOfMonth))"
            )
            .WithParameter("@dayOfWeek", (int)today.DayOfWeek + 1) // Cosmos DB days are 1-7
            .WithParameter("@dayOfMonth", today.Day);

            var iterator = container.GetItemQueryIterator<Objective>(query);
            var objectives = new List<Objective>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                objectives.AddRange(response.Resource);
            }

            return Ok(objectives);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAnObjectiveById(string id)
        {
            try
            {
                //var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", id);
                //var iterator = _container.GetItemQueryIterator<Objective>(query);
                //var result = await iterator.ReadNextAsync();
                //if (result.Resource.Count() > 0)
                //    return Ok(result.Resource);
                //else
                //    return NotFound();
                var result = await container.ReadItemAsync<Objective>(id, new PartitionKey(_defaultPartitionKey));
                return Ok(result.Resource);

            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
                else
                {
                    // For other errors, return a more generic message to the client
                    return StatusCode(500, "An error occurred while processing your request.");
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAnObjective(string id, Objective updatedObjective)
        {
            // Input Validation (Example)
            if (string.IsNullOrWhiteSpace(updatedObjective.Name))
            {
                return BadRequest("Objective name is required.");
            }

            // Check if the ID in the URL matches the ID in the object
            if (id != updatedObjective.Id)
            {
                return BadRequest("ID mismatch");
            }

            try
            {
                var response = await container.ReplaceItemAsync(updatedObjective, id, new PartitionKey(_defaultPartitionKey));
                return Ok(response.Resource);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnObjective(string id)
        {
            try
            {
                await container.DeleteItemAsync<Objective>(id, new PartitionKey(_defaultPartitionKey));
                return NoContent(); // 204 No Content on successful deletion
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }

        [HttpPatch("{id}/complete/{date}")]
        public async Task<IActionResult> MarkObjectiveAsCompleted(string id, DateOnly date)
        {
            try
            {
                var response = await container.ReadItemAsync<Objective>(id, new PartitionKey(_defaultPartitionKey));
                var objective = response.Resource;

                // Update completion history for today
                var completionEntry = objective.CompletionHistory.FirstOrDefault(e => e.Date == date);
                if (completionEntry != null)
                {
                    completionEntry.Completed = true ;
                }
                else
                {
                    objective.CompletionHistory.Add(new CompletionHistoryEntry(date, true));
                }

                // Replace the objective with the updated version
                response = await container.ReplaceItemAsync(objective, id, new PartitionKey(_defaultPartitionKey));
                return Ok(response.Resource);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }

        [HttpPatch("{id}/incomplete/{date}")]
        public async Task<IActionResult> MarkObjectiveAsIncomplete(string id, DateOnly date)
        {
            try
            {
                var response = await container.ReadItemAsync<Objective>(id, new PartitionKey(_defaultPartitionKey));
                var objective = response.Resource;

                // Update completion history for today
                var completionEntry = objective.CompletionHistory.FirstOrDefault(e => e.Date == date);
                if (completionEntry != null)
                {
                    completionEntry.Completed = false;
                }
                else
                {
                    objective.CompletionHistory.Add(new CompletionHistoryEntry(date, false));
                }

                // Replace the objective with the updated version
                response = await container.ReplaceItemAsync(objective, id, new PartitionKey(_defaultPartitionKey));
                return Ok(response.Resource);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }
    }
}
