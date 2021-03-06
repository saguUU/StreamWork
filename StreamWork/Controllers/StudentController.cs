﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StreamWork.Config;
using StreamWork.DataModels;
using StreamWork.HelperClasses;
using StreamWork.ViewModels;

namespace StreamWork.Controllers
{
    public class StudentController : Controller
    {
        HelperFunctions helperFunctions = new HelperFunctions();

        private readonly string _connectionString = "Server=tcp:streamwork.database.windows.net,1433;Initial Catalog=StreamWork;Persist Security Info=False;User ID=streamwork;Password=arizonastate1!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        public async Task<IActionResult> ProfileStudent([FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            var model = new UserProfile();
            var user = HttpContext.Session.GetString("UserProfile");
            var userProfile = await helperFunctions.GetUserProfile(storageConfig, QueryHeaders.CurrentUser, user);
            var splitName = userProfile.Name.Split(new char[] { '|' });
            model.FirstName = splitName[0];
            model.LastName = splitName[1];

            ProfileStudentViewModel viewModel = new ProfileStudentViewModel
            {
                userLogins = await helperFunctions.GetUserLogins(storageConfig, QueryHeaders.CurrentUser, user)
            };
            return View(viewModel);
        }

       [HttpGet]
        public async Task<IActionResult> ArchivedStreams([FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            var user = HttpContext.Session.GetString("UserProfile");
            ProfileStudentViewModel viewModel = new ProfileStudentViewModel
            {
                userLogins = await helperFunctions.GetUserLogins(storageConfig, QueryHeaders.CurrentUser, user),
                userArchivedStreams = await helperFunctions.GetArchivedStreams(storageConfig, QueryHeaders.UserArchivedVideos, user)
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ArchivedStreams([FromServices] IOptionsSnapshot<StorageConfig> storageConfig, string subject)
        {
            var user = HttpContext.Session.GetString("UserProfile");
            ProfileStudentViewModel viewModel = new ProfileStudentViewModel
            {
                userLogins = await helperFunctions.GetUserLogins(storageConfig, QueryHeaders.CurrentUser, user),
                userArchivedStreams = await helperFunctions.GetArchivedStreams(storageConfig, QueryHeaders.UserArchivedVideosBasedOnSubject, subject)
            };
            return View(viewModel);
        }

        public IActionResult StudentSettings()
        {
            return View();
        }
    }
}
