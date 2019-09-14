﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StreamWork.Models;
using Microsoft.AspNetCore.Http;
using StreamWork.Core;
using StreamWork.Config;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using StreamWork.ViewModels;
using System.Net.Mail;
using System.Net;

namespace StreamWork.Controllers
{
    public class HomeController : Controller
    {
        HelperFunctions helperFunctions = new HelperFunctions();


        public IActionResult Index()
        {
            if (Request.Host.ToString() == "streamwork.live")
            {
                return Redirect("https://www.streamwork.live");
            }
            return View();
        }

        public async Task<IActionResult> Math([FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            return View(await PopulateSubjectPage(storageConfig, "Mathematics"));
        }

        public async Task<IActionResult> Science([FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            return View(await PopulateSubjectPage(storageConfig, "Science"));
        }

        public async Task<IActionResult> Engineering([FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            return View(await PopulateSubjectPage(storageConfig, "Engineering"));
        }

        public async Task<IActionResult> Business([FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            return View(await PopulateSubjectPage(storageConfig, "Business"));
        }

        public async Task<IActionResult> Law([FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            return View(await PopulateSubjectPage(storageConfig, "Law"));
        }

        public async Task<IActionResult> DesignArt([FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            return View(await PopulateSubjectPage(storageConfig, "Art"));
        }

        public async Task<IActionResult> Humanities([FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            return View(await PopulateSubjectPage(storageConfig, "Humanities"));
        }

        public async Task<IActionResult> Other([FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            return View(await PopulateSubjectPage(storageConfig, "Other"));
        }

        public IActionResult BecomeTutor()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult HowToStream()
        {
            return View();
        }

        public IActionResult SplashPage()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ProfileView(string tutor, [FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            ProfileTutorViewModel profile = new ProfileTutorViewModel
            {
                userArchivedVideos = await helperFunctions.GetArchivedStreams(storageConfig, "UserArchivedVideos", tutor),
                userProfile = await helperFunctions.GetUserProfile(storageConfig, "CurrentUser", tutor)
            };
            return View(profile);
        }

        [HttpGet]
        public async Task<IActionResult> Donate(string Tutor, [FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            ProfileTutorViewModel profile = new ProfileTutorViewModel
            {
                userLogins = await helperFunctions.GetUserLogins(storageConfig, "PaticularSignedUpUsers", Tutor)
            };

            Donation donation = new Donation
            {
                Id = Guid.NewGuid().ToString(),
                Student = HttpContext.Session.GetString("UserProfile"),
                Tutor = profile.userLogins[0].Username,
                TimeSent = DateTime.Now.ToString(),
            };
            await helperFunctions.SaveDonation(storageConfig, donation);

            return View(profile);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<ProfileTutorViewModel> PopulateSubjectPage([FromServices] IOptionsSnapshot<StorageConfig> storageConfig, string subject)
        {
            var user = HttpContext.Session.GetString("UserProfile");

            ProfileTutorViewModel model = new ProfileTutorViewModel
            {
                userChannels = await helperFunctions.GetUserChannels(storageConfig, "AllUserChannelsThatAreStreaming", subject),
                userLogins = await GetPopularStreamTutors(storageConfig),
            };
            return model;
        }

        private async Task<List<UserLogin>> GetPopularStreamTutors([FromServices] IOptionsSnapshot<StorageConfig> storageConfig)
        {
            List<UserLogin> list = new List<UserLogin>();
            var getCurrentUsers = await DataStore.GetListAsync<UserLogin>(helperFunctions._connectionString, storageConfig.Value, "AllSignedUpUsers", null);
            foreach (UserLogin user in getCurrentUsers)
            {
                if (user.ProfileType.Equals("tutor"))
                {
                    list.Add(user);
                }
            }
            return list;
        }

        [HttpPost]
        public async Task<IActionResult> SignUp([FromServices] IOptionsSnapshot<StorageConfig> storageConfig,
                                                string nameFirst, string nameLast, string email, string phone, string username, string password, string passwordConfirm, string channelId, string role)
        {
            string confirmation = "";
            UserLogin signUpProflie = new UserLogin
            {
                Id = Guid.NewGuid().ToString(),
                Name = nameFirst + "|" + nameLast,
                EmailAddress = email,
                Username = username,
                Password = password,
                ProfileType = role,
                ProfilePicture = "https://streamworkblob.blob.core.windows.net/streamworkblobcontainer/default-profile.png"
            };

            UserChannel key = new UserChannel
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                ChannelKey = null,
                StreamSubject = null,
                StreamThumbnail = null,
                StreamTitle = null,
                ChatId = FormatChatId(DataStore.GetChatID("https://www.cbox.ws/apis/threads.php?id=6-829647-oq4rEn&key=ae1682707f17dbc2c473d946d2d1d7c3&act=mkthread"))
            };
            var checkCurrentUsers = await DataStore.GetListAsync<UserLogin>(helperFunctions._connectionString, storageConfig.Value, "CurrentUser", new List<string> { username });
            int numberOfUsers = 0;
            foreach (var user in checkCurrentUsers)
            {
                numberOfUsers++;
            }
            if (numberOfUsers != 0)
                confirmation = "Username already exsists";
            else if (password != passwordConfirm)
                confirmation = "Wrong Password";
            else
            {
                await DataStore.SaveAsync(helperFunctions._connectionString, storageConfig.Value, new Dictionary<string, object> { { "Id", signUpProflie.Id } }, signUpProflie);
                await DataStore.SaveAsync(helperFunctions._connectionString, storageConfig.Value, new Dictionary<string, object> { { "Id", key.Id } }, key);
                confirmation = "Success";
            }
            return Json(new { Message = confirmation });
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public IActionResult TryLogin([FromServices] IOptionsSnapshot<StorageConfig> storageConfig, string placeholder)
        {
            try
            {
                HttpContext.Session.GetString("UserProfile");
                if (HttpContext.Session.GetString("Tutor").Equals("true"))
                {
                    return Json(new { Message = "Welcome, StreamTutor" });
                }
                return Json(new { Message = "Welcome" });
            }
            catch
            {
                return Json(new { Message = "Wrong Password or Username " });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromServices] IOptionsSnapshot<StorageConfig> storageConfig, string username, string password)
        {
            string confirmation = "";
            if (storageConfig != null)
            {
                int user = 0;
                var checkforUser = await DataStore.GetListAsync<UserLogin>(helperFunctions._connectionString, storageConfig.Value, "AllSignedUpUsersWithPassword", new List<string> { username, password });
                foreach (var u in checkforUser)
                {
                    if (u.Password == password && u.Username == username)
                    {
                        user++;
                        if (u.ProfileType == "tutor")
                        {
                            user++;
                        }
                    }
                }
                if (user == 1)
                {
                    confirmation = "Welcome";
                    HttpContext.Session.SetString("UserProfile", username);
                    HttpContext.Session.SetString("Tutor", "false");
                }
                if (user == 2)
                {
                    confirmation = "Welcome, StreamTutor";
                    HttpContext.Session.SetString("UserProfile", username);
                    HttpContext.Session.SetString("Tutor", "true");
                }
            }
            else
            {
                confirmation = "Wrong Password or Username ";
            }
            return Json(new { Message = confirmation });
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        private string FormatChatId(string chatID)
        {
            var formattedphrase = chatID.Split(new char[] { '\t' });
            var formattedChatID = formattedphrase[2].Split(new char[] { '\n' });
            return formattedphrase[1] + "|" + formattedChatID[0];
        }

        [HttpGet]
        public IActionResult PasswordRecovery()
        {
            return View();
        }

        [HttpPost]
        public IActionResult PasswordRecovery(string username)
        { 

            return View();
        }
    }
}
