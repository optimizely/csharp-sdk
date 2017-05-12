﻿/* 
 * Copyright 2017, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using OptimizelySDK.DemoApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using OptimizelySDK.DemoApp.Utils;
using Newtonsoft.Json;

namespace OptimizelySDK.DemoApp.Controllers
{
    public class DemoController : Controller
    {
        private static Product[] ProductRepo = new[]
        {
            new Product { Id = 1, Name = "Long Sleeve Swing Shirt", Color = "Baby Blue", Category = "Shirts", Price = 54 },
            new Product { Id = 2, Name = "Bo Henry", Color = "Khaki", Category = "Shorts", Price = 37 },
            new Product { Id = 3, Name = "The \"Go\" Bag", Color = "Forest Green", Category = "Bags", Price = 118 },
            new Product { Id = 4, Name = "Springtime", Color = "Rose", Category = "Dresses", Price = 84 },
            new Product { Id = 5, Name = "The Night Out", Color = "Olive Green", Category = "Dresses", Price = 153 },
            new Product { Id = 6, Name = "Dawson Trolley", Color = "Pine Green", Category = "Shirts", Price = 107 },
        };

        private static Visitor[] VisitorRepo = new[]
        {
            new Visitor { Id = 10001, Name = "Mike", Age = 23 },
            new Visitor { Id = 10002, Name = "Ali", Age = 29 },
            new Visitor { Id = 10003, Name = "Sally", Age = 18 },
            new Visitor { Id = 10004, Name = "Jennifer", Age = 44 },
            new Visitor { Id = 10005, Name = "Randall", Age = 29 },
        };

        private static Config ConfigRepo = new Config();
        private static Optimizely Optimizely = null;
        private static InMemoryHandler InMemoryHandler = new InMemoryHandler();
        private static Logger.ILogger Logger = new MultiLogger(new[] { (Logger.ILogger)InMemoryHandler, new Log4NetLogger() });

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Messages()
        {
            return View(InMemoryHandler.HandlerItems);
        }


        [HttpGet]
        public ActionResult ClearMessages()
        {
            InMemoryHandler.Clear();
            return RedirectToAction("Messages");
        }

        [HttpGet]
        public ActionResult Config()
        {
            return View(ConfigRepo);
        }

        [HttpPost]
        public ActionResult Config(Config config)
        {
            ConfigRepo = config;
            string url = string.Format("https://cdn.optimizely.com/json/{0}.json", config.ProjectId);

            // will throw exception if invalid ProjectId
            using (var client = new System.Net.WebClient())
                config.ProjectConfigJson = FormatJson(client.DownloadString(url));

            Optimizely = new Optimizely(
                datafile: config.ProjectConfigJson,
                eventDispatcher: null,
                logger: Logger,
                errorHandler: InMemoryHandler,
                skipJsonValidation: false);

            return RedirectToAction("Config");
        }

        private static string FormatJson(string json)
        {
            // de-serialize, then re-serialize to format
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented);
        }

        private IEnumerable<Product> GetProducts(string sortVariation)
        {
            switch (sortVariation)
            {
                case "sort_by_price":
                    return ProductRepo.OrderBy(p => p.Price);

                default:
                case "sort_by_name":
                    return ProductRepo.OrderBy(p => p.Name);
            }
        }

        [HttpGet]
        public ActionResult Shop()
        {
            if (ConfigRepo == null || ConfigRepo.ExperimentKey == null)
                return RedirectToAction("Config");

            int? visitorId = (int?)Session["VisitorId"];
            if (visitorId == null)
                visitorId = VisitorRepo.First().Id;

            Visitor visitor = VisitorRepo.Single(v => v.Id == visitorId);
            var variation = Optimizely.Activate(
                experimentKey: ConfigRepo.ExperimentKey, 
                userId: visitor.Id.ToString(), 
                userAttributes: visitor.GetUserAttributes());

            return View(new DemoViewModel
            {
                Products = GetProducts(variation).ToArray(),
                CurrentVisitor = visitor,
                Message = TempData.ContainsKey("Message") ? (string)TempData["Message"] : null,
            });
        }

        [HttpGet]
        public ActionResult SelectVisitor(int? visitorId)
        {
            if (!visitorId.HasValue)
                return View(VisitorRepo);

            Session["VisitorId"] = visitorId.Value;
            return RedirectToAction("Shop");
        }


        [HttpGet] // should be a post, but keeping demo simple
        public ActionResult Buy(int visitorId, int productId)
        {
            // buy the item (record the conversion)
            var visitor = VisitorRepo.Single(v => v.Id == visitorId);
            Entity.EventTags eventTags = new Entity.EventTags();
            eventTags.Add("int_param", 4242);
            eventTags.Add("string_param", "4242");
            eventTags.Add("bool_param", true);
            eventTags.Add("revenue", 1337);

            Optimizely.Track("add_to_cart", Convert.ToString(visitorId), visitor.GetUserAttributes(), eventTags);
            TempData["Message"] = string.Format("Successfully Purchased item {0} for visitor {1}", productId, visitorId);

            return RedirectToAction("Shop");
        }
    }
}