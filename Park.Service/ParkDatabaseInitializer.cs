﻿using Microsoft.EntityFrameworkCore;
using Park.Models;
using Park.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;


namespace Park.Service
{
    /// <summary>
    /// 数据库初始化器
    /// </summary>
    public static class ParkDatabaseInitializer
    {
        /// <summary>
        /// 生成测试数据
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async static Task GenerateTestDatasAsync(ParkContext context)
        {

            List<ParkArea> parkAreas = new List<ParkArea>();
            Random r = new Random();
            await Config.SetAsync(context, "MonthlyPrice", "120");//设置月租120元/月
            PriceStrategy priceStrategy = new PriceStrategy()
            {
                StrategyJson = @"{
  ""type"": ""stepHourBase"", 
  ""prices"": [
    { 
      ""upper"": 5,
      ""price"": 2
    },
    {
      ""upper"": 12,
      ""price"": 1
    },
    {
      ""upper"": -1,
      ""price"": 0.5
    }
  ]
}",
                //MonthlyPrice = 120
            };
            context.PriceStrategys.Add(priceStrategy);

            //导入停车区信息
            parkAreas = await ParkingSpaceService.ImportFromJsonAsync(context, parkAreaJson);
            foreach (var parkArea in parkAreas)
            {
                parkArea.GateTokens = GenerateToken() + ";" + GenerateToken();
                parkArea.ParkingSpaces.ForEach(p => p.SensorToken = GenerateToken());
                parkArea.PriceStrategy = priceStrategy;
                //设置停车区的大门Token、生成每个停车位的Token、设置价格策略
            }
            //}
            //ParkArea parkArea = new ParkArea()
            //{
            //    Name = "停车场" + (i + 1),
            //    PriceStrategy = priceStrategy,
            //    Length = 100,
            //    Width = 50
            //};
            //    context.ParkAreas.Add(parkArea);
            //    for (int j = 0; j < r.Next(50, 100); j++)
            //    {
            //        context.ParkingSpaces.Add(new ParkingSpace()
            //        {
            //            ParkArea = parkArea,
            //            X = r.Next(0, 50),
            //            Y = r.Next(0, 50),
            //            Width = 5,
            //            Height = 2.5,
            //            RotateAngle = r.Next(0, 90)
            //        });
            //    }
            //}
            //context.SaveChanges();
            //var a = context.ParkAreas.First().ParkingSpaces;

            //parkAreas = await context.ParkAreas.ToListAsync();
            for (int i = 0; i < 20; i++)//车主
            {
                //注册一名车主
                var owner = (await CarOwnerService.Regist(context,
                    "user" + r.Next(0, short.MaxValue), "1234",
                    DateTime.Now.AddDays(-10).AddDays(-r.NextDouble() * 5))).CarOwner;//模拟用户在5天内注册的
                await context.SaveChangesAsync();

                //模拟为车主充值5次
                await TransactionService.RechargeMoneyAsync(context, owner.ID, r.Next(20, 200),
                    DateTime.Now.AddDays(-7).AddDays(-r.NextDouble()));
                await TransactionService.RechargeMoneyAsync(context, owner.ID, r.Next(10, 50),
                        DateTime.Now.AddDays(-6).AddDays(-r.NextDouble()));
                await TransactionService.RechargeMoneyAsync(context, owner.ID, r.Next(30, 200),
                        DateTime.Now.AddDays(-5).AddDays(-r.NextDouble()));

                int carCount = r.Next(2, 5);
                for (int j = 0; j < carCount; j++)//车辆
                {
                    //为车主加入几辆车
                    var car = new Car()
                    {
                        LicensePlate = "浙B" + r.Next(10000, 99999),
                        CarOwner = owner
                    };
                    context.Cars.Add(car);
                    context.SaveChanges();

                    int parkCount = r.Next(2, 10);
                    //为每辆车模拟生成几次停车记录
                    for (int k = 0; k < parkCount; k++)//进出场信息
                    {
                        DateTime enterTime = DateTime.Now.AddDays(-parkCount + k - 1 - r.NextDouble());
                        DateTime leaveTime = enterTime.AddDays(r.NextDouble());
                        var parkArea = parkAreas[r.Next(0, 2)];
                        //模拟用户在5天内进入过停车场，然后出了停车场
                        await ParkService.EnterAsync(context, car.LicensePlate, parkArea, enterTime);
                        var result = await ParkService.LeaveAsync(context, car.LicensePlate, parkArea, leaveTime);
                        //这里非常奇怪，部分停车记录的停车时间会变成1-01-01 8:05，明明ParkRecord里的时间都是正确的
                        //已解决，时间问题
                    }
                }
            }

            context.SaveChanges();
        }

        /// <summary>
        /// 保证数据库已初始化
        /// </summary>
        /// <param name="context"></param>
        public static void Initialize(ParkContext context)
        {
            context.Database.EnsureCreated();
        }
        /// <summary>
        /// 生成随机Token
        /// </summary>
        /// <returns></returns>
        private static string GenerateToken()
        {
            return Guid.NewGuid().ToString().Split('-')[0];
        }
        private static string parkAreaJson = @"
[{""Name"":""停车场1"",""Width"":20,""Length"":40,""ParkingSpaces"":[{""ID"":0,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":10.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":1,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":10.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":2,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":13.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":3,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":16.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":4,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":19.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":5,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":22.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":6,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":25.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":7,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":25.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":8,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":22.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":9,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":13.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":10,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":16.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":11,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":19.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":12,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":31.0,""Y"":3.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":0.0,""SensorToken"":null},{""ID"":13,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":7.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":-90.0,""SensorToken"":null},{""ID"":14,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":4.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":-90.0,""SensorToken"":null}],""Aisles"":[{""X1"":1.5,""Y1"":5.5,""X2"":9.0,""Y2"":5.5,""ID"":0,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":8.0,""Y1"":5.5,""X2"":8.0,""Y2"":10.0,""ID"":1,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":7.0,""Y1"":10.0,""X2"":30.5,""Y2"":10.0,""ID"":2,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":29.5,""Y1"":10.0,""X2"":29.5,""Y2"":3.5,""ID"":3,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null}],""Walls"":[{""X1"":3.0,""Y1"":2.0,""X2"":38.0,""Y2"":2.0,""ID"":0,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":30.5,""Y1"":17.5,""X2"":3.0,""Y2"":17.5,""ID"":5,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":4.0,""Y1"":17.5,""X2"":4.0,""Y2"":8.5,""ID"":6,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":32.0,""Y1"":8.0,""X2"":32.0,""Y2"":18.5,""ID"":12,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":32.0,""Y1"":17.5,""X2"":3.0,""Y2"":17.5,""ID"":14,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":33.0,""Y1"":17.5,""X2"":3.0,""Y2"":17.5,""ID"":18,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":31.0,""Y1"":8.0,""X2"":38.0,""Y2"":8.0,""ID"":19,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":37.0,""Y1"":2.0,""X2"":37.0,""Y2"":8.0,""ID"":20,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null}],""ID"":0,""PriceStrategyID"":null,""PriceStrategy"":null},
{""Name"":""停车场2"",""Width"":20,""Length"":40,""ParkingSpaces"":[{""ID"":0,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":10.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":1,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":10.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":2,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":13.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":3,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":16.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":4,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":19.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":5,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":22.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":6,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":25.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":7,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":25.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":8,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":22.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":9,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":13.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":10,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":16.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":11,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":19.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":12,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":31.0,""Y"":3.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":0.0,""SensorToken"":null},{""ID"":13,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":7.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":-90.0,""SensorToken"":null},{""ID"":14,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":4.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":-90.0,""SensorToken"":null}],""Aisles"":[{""X1"":1.5,""Y1"":5.5,""X2"":9.0,""Y2"":5.5,""ID"":0,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":8.0,""Y1"":5.5,""X2"":8.0,""Y2"":10.0,""ID"":1,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":7.0,""Y1"":10.0,""X2"":30.5,""Y2"":10.0,""ID"":2,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":29.5,""Y1"":10.0,""X2"":29.5,""Y2"":3.5,""ID"":3,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null}],""Walls"":[{""X1"":3.0,""Y1"":2.0,""X2"":38.0,""Y2"":2.0,""ID"":0,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":30.5,""Y1"":17.5,""X2"":3.0,""Y2"":17.5,""ID"":5,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":4.0,""Y1"":17.5,""X2"":4.0,""Y2"":8.5,""ID"":6,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":32.0,""Y1"":8.0,""X2"":32.0,""Y2"":18.5,""ID"":12,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":32.0,""Y1"":17.5,""X2"":3.0,""Y2"":17.5,""ID"":14,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":33.0,""Y1"":17.5,""X2"":3.0,""Y2"":17.5,""ID"":18,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":31.0,""Y1"":8.0,""X2"":38.0,""Y2"":8.0,""ID"":19,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":37.0,""Y1"":2.0,""X2"":37.0,""Y2"":8.0,""ID"":20,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null}],""ID"":0,""PriceStrategyID"":null,""PriceStrategy"":null},
{""Name"":""停车场3"",""Width"":20,""Length"":40,""ParkingSpaces"":[{""ID"":0,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":10.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":1,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":10.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":2,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":13.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":3,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":16.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":4,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":19.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":5,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":22.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":6,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":25.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":7,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":25.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":8,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":22.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":9,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":13.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":10,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":16.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":11,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":19.0,""Y"":4.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":90.0,""SensorToken"":null},{""ID"":12,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":31.0,""Y"":3.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":0.0,""SensorToken"":null},{""ID"":13,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":7.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":-90.0,""SensorToken"":null},{""ID"":14,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null,""HasCar"":false,""X"":4.0,""Y"":13.0,""Width"":4.5,""Height"":2.5,""RotateAngle"":-90.0,""SensorToken"":null}],""Aisles"":[{""X1"":1.5,""Y1"":5.5,""X2"":9.0,""Y2"":5.5,""ID"":0,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":8.0,""Y1"":5.5,""X2"":8.0,""Y2"":10.0,""ID"":1,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":7.0,""Y1"":10.0,""X2"":30.5,""Y2"":10.0,""ID"":2,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":29.5,""Y1"":10.0,""X2"":29.5,""Y2"":3.5,""ID"":3,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null}],""Walls"":[{""X1"":3.0,""Y1"":2.0,""X2"":38.0,""Y2"":2.0,""ID"":0,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":30.5,""Y1"":17.5,""X2"":3.0,""Y2"":17.5,""ID"":5,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":4.0,""Y1"":17.5,""X2"":4.0,""Y2"":8.5,""ID"":6,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":32.0,""Y1"":8.0,""X2"":32.0,""Y2"":18.5,""ID"":12,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":32.0,""Y1"":17.5,""X2"":3.0,""Y2"":17.5,""ID"":14,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":33.0,""Y1"":17.5,""X2"":3.0,""Y2"":17.5,""ID"":18,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":31.0,""Y1"":8.0,""X2"":38.0,""Y2"":8.0,""ID"":19,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null},{""X1"":37.0,""Y1"":2.0,""X2"":37.0,""Y2"":8.0,""ID"":20,""Class"":"""",""ParkAreaID"":0,""ParkArea"":null}],""ID"":0,""PriceStrategyID"":null,""PriceStrategy"":null}]";
    }
}