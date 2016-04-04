﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskCat.Tests.Order
{
    using NUnit.Framework;
    using Data.Model.Order;
    using Data.Model;
    using Data.Model.GeoJson;
    using Data.Lib.Constants;

    [TestFixture]
    public class DeliveryOrderTests
    {
        [Test]
        public void TestDeliveryOrderCreationWithName()
        {
            Location FromLocation = new Location() { Address = "Test From Address", Point = new Point((new double[] { 1, 2 }).ToList()) };

            Location ToLocation = new Location() { Address = "Test To Address", Point = new Point((new double[] { 2, 1 }).ToList()) };
            string orderName = "Test Delivery Order";
            string PackageDescription = "Sample Package Description";
            double PackageWeight = 100;

            DeliveryOrder order = new DeliveryOrder(orderName);
            order.From = FromLocation;
            order.To = ToLocation;
            order.PackageDescription = PackageDescription;
            order.PackageWeight = PackageWeight;
                  
            Assert.IsNotNull(order);
            Assert.AreEqual(FromLocation, order.From);
            Assert.AreEqual(ToLocation, order.To);
            Assert.AreEqual(orderName, order.Name);
            Assert.AreEqual(PackageDescription, order.PackageDescription);
            Assert.AreEqual(PackageWeight, order.PackageWeight);
            Assert.AreEqual("default", order.PayloadType);
            Assert.AreEqual(JobTaskTypes.DELIVERY, order.Type);
            Assert.Null(order.ETA);
            Assert.Null(order.ETAMinutes);
            Assert.AreEqual("Anonymous", order.User);
        }
    }
}