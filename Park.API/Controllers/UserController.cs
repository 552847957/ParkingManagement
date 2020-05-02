﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Park.Models;
using Park.Service;


namespace Park.API.Controllers
{
    [ApiController]
    [EnableCors("cors")]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        Context db;
        public UserController()
        {
            db = new Context();
        }

        [HttpPost]
        [Route("Login")]
        public async Task<ResponseData<LoginResult>> LoginAsync([FromBody] LoginRequest request)
        {
            var a = await db.CarOwners.FirstAsync();
            var result = await CarOwnerService.Login(db, request.Username, request.Password);
            switch (result.Type)
            {
                case LoginOrRegisterResultType.Succeed:
                    var r = new LoginResult(result.CarOwner);
                    HttpContext.Session.SetInt32("user", r.CarOwner.ID);
                    return new ResponseData<LoginResult>()
                    {
                        Message = "登陆成功",
                        Data = r
                    };
                case LoginOrRegisterResultType.Empty:
                    return new ResponseData<LoginResult>()
                    {
                        Message = "用户名或密码为空",
                    };
                case LoginOrRegisterResultType.Wrong:
                    return new ResponseData<LoginResult>()
                    {
                        Message = "用户名或密码错误",
                    };
                default:
                    throw new NotImplementedException();
            }
        }
        [HttpPost]
        [Route("Register")]
        public async Task<ResponseData<LoginResult>> RegisterAsync([FromBody] LoginRequest request)
        {
            var a = await db.CarOwners.FirstAsync();
            var result = await CarOwnerService.Register(db, request.Username, request.Password);
            switch (result.Type)
            {
                case LoginOrRegisterResultType.Succeed:
                    var r = new LoginResult(result.CarOwner);
                    HttpContext.Session.SetInt32("user", r.CarOwner.ID);
                    return new ResponseData<LoginResult>()
                    {
                        Message = "注册成功",
                        Data = r
                    };
                case LoginOrRegisterResultType.Existed:
                    return new ResponseData<LoginResult>()
                    {
                        Message = "用户名已存在",
                    };
                default:
                    throw new NotImplementedException();
            }
        }
        [HttpPost]
        [Route("home")]
        public async Task<ResponseData<OverviewResponse>> OverviewAsync([FromBody] UserToken request)
        {
            if (!request.IsValid())
            {
                return new ResponseData<OverviewResponse>() { Succeed = false, Message = "用户验证失败" };
            }
            OverviewResponse response = new OverviewResponse();
            foreach (var car in await db.Cars.Where(p => p.CarOwnerID == request.UserID).ToListAsync())
            {
                int recordCount = await db.ParkRecords.CountAsync(p => p.CarID == car.ID);
                response.Cars.Add(new { car.LicensePlate, Records = recordCount,car.ID });
            }
            TransactionRecord transaction = await db.TransactionRecords
                .LastOrDefaultRecordAsync(p => p.Time, p => p.CarOwnerID == request.UserID);
            response.Balance = transaction.Balance;
            response.ExpireTime = transaction.ExpireTime.ToShortDateString();

            return new ResponseData<OverviewResponse>() { Data = response };
        }
        [HttpPost]
        [Route("Car")]
        public async Task<ResponseData<object>> CarAsync([FromBody] CarRequest request)
        {
            if (!request.IsValid())
            {
                return new ResponseData<object>() { Succeed = false, Message = "用户验证失败" };
            }
            int carOwnerID = request.UserID;
            Car car = null;
            switch (request.Type)
            {
                case "add":
                    car = new Car()
                    {
                        CarOwnerID = carOwnerID,
                        Enabled = true,
                        LicensePlate = request.LicensePlate
                    };
               
                    db.Cars.Add(car);
                    await db.SaveChangesAsync();
                    return new ResponseData<object>() { Data = true };
                case "detail":
                  car= await db.Cars.Include(p => p.ParkRecords).ThenInclude(p=>p.ParkArea)
                        .Include(p => p.ParkRecords).ThenInclude(p => p.TransactionRecord)
                        .FirstOrDefaultAsync(p => p.ID == request.CarID && p.CarOwnerID == request.UserID);
                 
                    return new ResponseData<object>() { Data = car };
                case "delete":
                    car= await db.Cars.FirstOrDefaultAsync(p => p.ID == request.CarID
                    && p.CarOwnerID==request.UserID);
                    if (car != null)
                    {
                        db.Cars.Remove(car);
                        await db.SaveChangesAsync();
                    }
                    return new ResponseData<object>() { Data = true };
                default:
                    throw new NotImplementedException();
            }
        }

    }

    public class OverviewResponse
    {
        public List<dynamic> Cars { get; set; } = new List<dynamic>();//包括车牌和停车次数
        public double Balance { get; set; }
        public string ExpireTime { get; set; }
    }
    public class LoginRequest
    {
        public string Password { get; set; }
        public string Username { get; set; }
    }
    public class CarRequest : UserToken
    {
        public string Type { get; set; }//add/edit/delete/detail
        public int CarID { get; set; }
        public string LicensePlate { get; set; }
    }
    public class LoginResult : UserToken
    {
        public LoginResult(CarOwner carOwner) : base(carOwner.ID, true)
        {
            CarOwner = carOwner;
            carOwner.Password = null;
        }

        public CarOwner CarOwner { get; set; }
    }

    public class UserToken
    {
        public int UserID { get; set; }
        public string Token { get; set; }
        private const string Key = "ParkKey";

        public UserToken()
        { }
        public UserToken(int userID, bool createToken)
        {
            UserID = userID;
            if (createToken)
            {
                Token = GetToken();
            }
        }

        public bool IsValid()
        {
            var aes = new FzLib.Cryptography.Aes();
            aes.SetStringKey(Key + UserID);
            aes.SetStringIV("");
            try
            {
                string[] items = aes.Decrypt(Token).Split("-");
                if (items[0] != UserID.ToString())
                {
                    return false;
                }
                //预留过期检测
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public string GetToken()
        {
            var aes = new FzLib.Cryptography.Aes();
            aes.SetStringKey(Key + UserID);
            aes.SetStringIV("");
            return aes.Encrypt(string.Join("-", UserID.ToString(), DateTime.Now.ToString("yyyyMMdd")));
        }
    }
}
