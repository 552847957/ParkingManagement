﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Park.Mgt.Models;

using FineUICore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Park.Mgt.Pages.Admin
{
    [CheckPower(Name = "CoreMenuEdit")]
    public class MenuEditModel : BaseAdminModel
    {
        [BindProperty]
        public Models.Menu Menu { get; set; }

        public IEnumerable<Models.Menu> Menus { get; set; }

        public RadioItem[] IconItems { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Menu = await DB.Menus
                .Include(m => m.Parent)
                .Include(m => m.ViewPower)
                .Where(m => m.ID == id).FirstOrDefaultAsync();

            if (Menu == null)
            {
                return Content("无效参数！");
            }

            MenuEdit_LoadData(id);

            return Page();
        }


        private void MenuEdit_LoadData(int id)
        {
            IconItems = MenuEdit_GetIconItems().ToArray();

            Menus = ResolveDDL<Models.Menu>(MenuHelper.Menus, id).ToArray();

        }

        public List<RadioItem> MenuEdit_GetIconItems()
        {
            List<RadioItem> items = new List<RadioItem>();

            string[] icons = new string[] { "tag_yellow", "tag_red", "tag_purple", "tag_pink", "tag_orange", "tag_green", "tag_blue" };
            foreach (string icon in icons)
            {
                string value = String.Format("~/res/icon/{0}.png", icon);
                string text = String.Format("<img style=\"vertical-align:bottom;\" src=\"{0}\" />&nbsp;{1}", Url.Content(value), icon);

                items.Add(new RadioItem(text, value));
            }

            return items;
        }

        public async Task<IActionResult> OnPostMenuEdit_btnSaveClose_ClickAsync(string ViewPowerName)
        {
            if (ModelState.IsValid)
            {
                // 下拉列表的顶级节点值为-1
                if (Menu.ParentID == -1)
                {
                    Menu.ParentID = null;
                }

                if (String.IsNullOrEmpty(ViewPowerName))
                {
                    Menu.ViewPowerID = null;
                }
                else
                {
                    var viewPower = await DB.Powers
                        .Where(p => p.Name == ViewPowerName)
                        .FirstOrDefaultAsync();

                    if (viewPower != null)
                    {
                        Menu.ViewPowerID = viewPower.ID;
                    }
                    else
                    {
                        Alert.Show("浏览权限 " + ViewPowerName + " 不存在！");
                        return UIHelper.Result();
                    }
                }

                DB.Entry(Menu).State = EntityState.Modified;
                await DB.SaveChangesAsync();

                // 关闭本窗体（触发窗体的关闭事件）
                ActiveWindow.HidePostBack();
            }

            return UIHelper.Result();

        }
    }
}