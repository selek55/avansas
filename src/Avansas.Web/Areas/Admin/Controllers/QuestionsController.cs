using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class QuestionsController : Controller
{
    private readonly IProductQuestionService _questionService;

    public QuestionsController(IProductQuestionService questionService) => _questionService = questionService;

    public async Task<IActionResult> Index()
    {
        var questions = await _questionService.GetAllQuestionsAsync();
        return View(questions);
    }

    [HttpPost]
    public async Task<IActionResult> Answer(int questionId, string answerText)
    {
        await _questionService.AnswerQuestionAsync(questionId, answerText);
        TempData["Success"] = "Soru yanıtlandı";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int questionId)
    {
        await _questionService.ApproveQuestionAsync(questionId);
        TempData["Success"] = "Soru onaylandı";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int questionId)
    {
        await _questionService.DeleteQuestionAsync(questionId);
        TempData["Success"] = "Soru silindi";
        return RedirectToAction(nameof(Index));
    }
}
