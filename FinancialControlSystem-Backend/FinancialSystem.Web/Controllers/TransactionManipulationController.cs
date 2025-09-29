using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialSystem.Web.Controllers
{
    [Route("api/transaction")]
    [ApiController]
    public class TransactionManipulationController : Controller
    {
        private readonly ITransactionAppService _transactionAppService;

        public TransactionManipulationController(ITransactionAppService transactionAppService)
        {
            _transactionAppService = transactionAppService;
        }

        [HttpPost("create/planned")]
        [Authorize]
        public async Task<IActionResult> PlannedTransactionCreationController(TransactionDataDto input)
        {
            try
            {
                await _transactionAppService.InsertPlannedTransaction(input);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("create/unplanned")]
        [Authorize]
        public async Task<IActionResult> UnplannedTransactionCreationController(TransactionDataDto input)
        {
            try
            {
                await _transactionAppService.InsertUnplannedTransaction(input);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/planned")]
        [Authorize]
        public async Task<IActionResult> PlannedTransactionUpdateController(TransactionDataDto input)
        {
            try
            {
                await _transactionAppService.UpdatePlannedTransaction(input);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/unplanned")]
        [Authorize]
        public async Task<IActionResult> UnplannedTransactionUpdateController(TransactionDataDto input)
        {
            try
            {
                await _transactionAppService.UpdateUnplannedTransaction(input);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete/transaction")]
        [Authorize]
        public async Task<IActionResult> TransactionExclusionController(Guid input)
        {
            try
            {
                await _transactionAppService.DeleteTransaction(input);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get/all/planned")]
        [Authorize]
        public async Task<IActionResult> GetAllPlannedTransactionInList()
        {
            try
            {
                return Ok(await _transactionAppService.GetAllPlannedTransactions());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get/all/unplanned")]
        [Authorize]
        public async Task<IActionResult> GetAllUnplannedTransactionInList()
        {
            try
            {
                return Ok(await _transactionAppService.GetAllUnplannedTransactions());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/totalBalance")]
        [Authorize]
        public async Task<IActionResult> TotalBalanceUpdateController()
        {
            try
            {
                await _transactionAppService.UpdateEnvironmentBalance();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}