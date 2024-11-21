using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartTwoProg.Data;
 
using PartTwoProg.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class ClaimsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly long _maxFileSize = 5 * 1024 * 1024;
    private readonly string[] _allowedExtensions = { ".pdf", ".docx", ".xlsx" };


    public ClaimsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    [HttpGet]
    public IActionResult SubmitClaim()
    {
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> SubmitClaim(Claim claim, IFormFile document)
    {
        var userId = User.Identity.Name; // Assuming User.Identity.Name contains the username or unique ID.
        claim.UserId = userId;

        // Define the salary limit (adjustable as needed)
        const decimal salaryLimit = 5000M;

        // Check if claim exceeds limit
        if (Claim.IsOverLimit(claim.HourlyRate, claim.HoursWorked, salaryLimit))
        {
            claim.Status = "Rejected";
            ModelState.AddModelError(string.Empty, "The claim exceeds the allowed salary limit and has been automatically rejected.");
        }
        else
        {
            claim.Status = "Pending"; // Default status for valid claims
        }

        if (document != null && document.Length > 0)
        {
            var fileExtension = Path.GetExtension(document.FileName).ToLower();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("document", "Invalid file type. Only PDF, DOCX, and XLSX files are allowed.");
                return View(claim);
            }

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var filePath = Path.Combine(uploadsPath, document.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await document.CopyToAsync(stream);
            }

            claim.SupportingDocument = $"/uploads/{document.FileName}";
        }
        else
        {
            ModelState.AddModelError("document", "Please upload a supporting document.");
            return View(claim);
        }

        _dbContext.Claims.Add(claim);
        await _dbContext.SaveChangesAsync();

        return RedirectToAction("ClaimSubmitted");
    }


    public IActionResult ClaimSubmitted()
    {
        return View();
    }


    [Authorize(Roles = "Coordinator,Manager")]
    [HttpGet]
    public async Task<IActionResult> ViewPendingClaims()
    {
        try
        {
            var pendingClaims = await _dbContext.Claims.Where(c => c.Status == "Pending").ToListAsync();
            return View(pendingClaims);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while fetching the claims. Please try again later.");
            Console.WriteLine(ex.Message);
            return View("Error");
        }
    }


    [Authorize(Roles = "Co-ordinator,Manager")]
    [HttpPost]
    public async Task<IActionResult> ApproveClaim(int id)
    {
        try
        {
            var claim = await _dbContext.Claims.FindAsync(id);
            if (claim != null)
            {
                claim.Status = "Approved";
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Claim not found.");
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while approving the claim. Please try again.");
            Console.WriteLine(ex.Message);
        }
        return RedirectToAction("ViewPendingClaims");
    }


    [Authorize(Roles = "Coordinator,Manager")]
    [HttpPost]
    public async Task<IActionResult> RejectClaim(int id)
    {
        try
        {
            var claim = await _dbContext.Claims.FindAsync(id);
            if (claim != null)
            {
                claim.Status = "Rejected";
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Claim not found.");
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while rejecting the claim. Please try again.");
            Console.WriteLine(ex.Message);
        }
        return RedirectToAction("ViewPendingClaims");
    }

    [Authorize(Roles = "Coordinator,Manager,Lecturer")]
    [HttpGet]
    public async Task<IActionResult> TrackClaims()
    {
        try
        {
            var userId = User.Identity.Name;

            // Admin roles can see all claims
            if (User.IsInRole("Coordinator") || User.IsInRole("Manager"))
            {
                var allClaims = await _dbContext.Claims.ToListAsync();
                return View(allClaims);
            }

            // Lecturers see only their claims
            var userClaims = await _dbContext.Claims.Where(c => c.UserId == userId).ToListAsync();
            return View(userClaims);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while fetching the claims. Please try again later.");
            Console.WriteLine(ex.Message);
            return View("Error");
        }
    }

    [Authorize(Roles = "Coordinator,Manager")]
    [HttpPost]
    public async Task<IActionResult> DeleteClaim(int id)
    {
        var claim = await _dbContext.Claims.FindAsync(id);
        if (claim != null)
        {
            _dbContext.Claims.Remove(claim);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("TrackClaims");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Claim not found.");
            return View("Error");
        }
    }

    [Authorize(Roles = "HR")]
    [HttpGet]
    public async Task<IActionResult> HRView()
    {
        try
        {
            var allClaims = await _dbContext.Claims.ToListAsync();
            return View(allClaims);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while fetching the claims for HR view.");
            Console.WriteLine(ex.Message);
            return View("Error");
        }
    }

    [Authorize(Roles = "HR")]
    [HttpGet]
    public async Task<IActionResult> GenerateClaimReport()
    {
        try
        {
            // Filter to include only approved claims
            var approvedClaims = await _dbContext.Claims.Where(c => c.Status == "Approved").ToListAsync();

            // Generate the report content
            string reportContent = "Claim ID, Lecturer Name, Hours Worked, Hourly Rate, Total Salary, Status\n";
            foreach (var claim in approvedClaims)
            {
                reportContent += $"{claim.Id}, {claim.LecturerName}, {claim.HoursWorked}, {claim.HourlyRate}, {claim.TotalSalary}, {claim.Status}\n";
            }

            // Return the report as a downloadable file
            byte[] reportBytes = System.Text.Encoding.UTF8.GetBytes(reportContent);
            return File(reportBytes, "text/csv", "ApprovedClaimReport.csv");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while generating the claim report.");
            Console.WriteLine(ex.Message);
            return View("Error");
        }
    }
}