﻿/*
 * Copyright 2016 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : ScadaWebCommon
 * Summary  : Content accessible to the web application user
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using Scada.Client;
using Scada.Data.Models;
using Scada.Web.Plugins;
using System;
using System.Collections.Generic;
using Utils;

namespace Scada.Web.Shell
{
    /// <summary>
    /// Content accessible to the web application user
    /// <para>Контент, доступный пользователю веб-приложения</para>
    /// </summary>
    public class UserContent
    {
        /// <summary>
        /// Журнал
        /// </summary>
        protected readonly Log log;


        /// <summary>
        /// Конструктор, ограничивающий создание объекта без параметров
        /// </summary>
        protected UserContent()
        {
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public UserContent(Log log)
        {
            if (log == null)
                throw new ArgumentNullException("log");

            this.log = log;
            ReportItems = new List<ReportItem>();
            DataWndItems = new List<DataWndItem>();
        }


        /// <summary>
        /// Получить элементы отчётов, доступные пользователю
        /// </summary>
        public List<ReportItem> ReportItems { get; protected set; }

        /// <summary>
        /// Получить элементы окон данных, доступные пользователю
        /// </summary>
        public List<DataWndItem> DataWndItems { get; protected set; }


        /// <summary>
        /// Добавление контента, прописанного в базе конфигурации
        /// </summary>
        protected void AddContentFromBase(UserRights userRights, Dictionary<string, UiObjSpec> uiObjSpecs, 
            DataAccess dataAccess)
        {
            if (userRights != null && uiObjSpecs != null)
            {
                List<UiObjProps> uiObjPropsList = dataAccess.GetUiObjPropsList(
                    UiObjProps.BaseUiTypes.Report | UiObjProps.BaseUiTypes.DataWnd);

                foreach (UiObjProps uiObjProps in uiObjPropsList)
                {
                    int uiObjID = uiObjProps.UiObjID;

                    if (userRights.GetUiObjRights(uiObjID).ViewRight)
                    {
                        UiObjSpec uiObjSpec;
                        uiObjSpecs.TryGetValue(uiObjProps.TypeCode, out uiObjSpec);

                        if (uiObjProps.BaseUiType == UiObjProps.BaseUiTypes.Report)
                        {
                            // добавление элемента отчёта
                            ReportItem reportItem = new ReportItem()
                            {
                                UiObjID = uiObjID,
                                Text = uiObjProps.Title
                            };

                            if (uiObjSpec is ReportSpec)
                            {
                                ReportSpec reportSpec = (ReportSpec)uiObjSpec;
                                if (string.IsNullOrEmpty(reportItem.Text))
                                    reportItem.Text = reportSpec.Name;
                                reportItem.Url = uiObjSpec.GetUrl(uiObjID);
                                reportItem.ReportSpec = reportSpec;
                            }

                            ReportItems.Add(reportItem);
                        }
                        else if (uiObjProps.BaseUiType == UiObjProps.BaseUiTypes.DataWnd)
                        {
                            // добавление элемента окна данных
                            DataWndItem dataWndItem = new DataWndItem()
                            {
                                UiObjID = uiObjID,
                                Text = uiObjProps.Title
                            };

                            if (uiObjSpec is DataWndSpec)
                            {
                                DataWndSpec dataWndSpec = (DataWndSpec)uiObjSpec;
                                if (string.IsNullOrEmpty(dataWndItem.Text))
                                    dataWndItem.Text = dataWndSpec.Name;
                                dataWndItem.Url = uiObjSpec.GetUrl(uiObjID);
                                dataWndItem.DataWndSpec = dataWndSpec;
                            }

                            DataWndItems.Add(dataWndItem);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Добавление контента, доступного всем, который задаётся спецификациями плагинов
        /// </summary>
        protected void AddContentFromPlugins(List<PluginSpec> pluginSpecs)
        {
            if (pluginSpecs != null)
            {
                foreach (PluginSpec pluginSpec in pluginSpecs)
                {
                    // добавление общедоступных элементов отчётов
                    if (pluginSpec.ReportSpecs != null)
                    {
                        foreach (ReportSpec reportSpec in pluginSpec.ReportSpecs)
                        {
                            if (reportSpec.ForEveryone)
                            {
                                ReportItems.Add(new ReportItem()
                                {
                                    Text = reportSpec.Name,
                                    Url = reportSpec.Url,
                                    ReportSpec = reportSpec
                                });
                            }
                        }
                    }

                    // добавление общедоступных элементов окон данных
                    if (pluginSpec.DataWndSpecs != null)
                    {
                        foreach (DataWndSpec dataWndSpec in pluginSpec.DataWndSpecs)
                        {
                            if (dataWndSpec.ForEveryone)
                            {
                                DataWndItems.Add(new DataWndItem()
                                {
                                    Text = dataWndSpec.Name,
                                    Url = dataWndSpec.Url,
                                    DataWndSpec = dataWndSpec
                                });
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Инициализировать доступный контент пользователя
        /// </summary>
        public void Init(UserData userData, DataAccess dataAccess)
        {
            if (userData == null)
                throw new ArgumentNullException("userData");

            try
            {
                ReportItems.Clear();
                DataWndItems.Clear();

                AddContentFromBase(userData.UserRights, userData.UiObjSpecs, dataAccess);
                AddContentFromPlugins(userData.PluginSpecs);

                ReportItems.Sort();
                DataWndItems.Sort();
            }
            catch (Exception ex)
            {
                log.WriteException(ex, Localization.UseRussian ?
                    "Ошибка при инициализации доступного контента пользователя" :
                    "Error initializing accessible user content");
            }
        }
    }
}
