using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Mask;
using DevExpress.XtraGrid;
using DevExpress.XtraLayout;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Nodes;
using FozzySystems;
using FozzySystems.Controls;
using FozzySystems.Helpers;
using FozzySystems.Proxy;
using FozzySystems.Types;using FozzySystems.Types.Contracts;
using FozzySystems.Utils;
using FZComponents.Common;
using FZComponents.Controls;
using FozzySystems.Types.Filtering;

namespace FZComponents.Dialogs
{
    public partial class MasterDataGoodsFilterWithLagerParams : XtraForm
    {

        private const string FilterSettingsKey = "FilterSettings";
        private const string SaveSettingsKey = "SaveSettings";

        public static DataSet MasterDataSet { get; set; }

        public Signs SignFilter { get; set; }
       
        private List<string> _filterHint;
        public CacheControls CacheAllControls { get; set; }

        public SaveParameters CacheParameters { get; set; }

        public string FilterHint
        {
            get
            {
                return ((_filterHint == null) || (_filterHint.Count == 0))
                    ? "Все товары"
                    : String.Join("; ", _filterHint.ToArray());
            }
        }

        private List<SignFilterValue> Parameters { get; set; }
        private bool useLikeForEKT;

        public MasterDataGoodsFilterWithLagerParams(string operationName, string goodsRequest = null, bool useLikeForEKT = false)
        {
            InitializeComponent();

            this.useLikeForEKT = useLikeForEKT;

            FormClosing += (s, e) =>
            {
                // если форму закрыли в процессе загрузки мастерданных,
                // то отменим запрос
                if (_masterDataRequestGuid != Guid.Empty)
                {
                    FZCoreProxy.CancelConnection(_masterDataRequestGuid);
                }
            };

            if (!String.IsNullOrEmpty(goodsRequest))
            {
                this.goodsRequest = goodsRequest;
            }

            LoadDictionaryFinishedEvent += RemoveChaseControlOnFilter;

            LoadGoodsFinishedEvent += RemoveChaseControlOnSerch;

            OperationName = operationName;

            LoadCacheParameters();

            LoadControlParams();

            AddChaseControlOnFilter();
            GetGoodsDataDictionary();

            IsChecked = true;
            IsClearData = true;
            IsFromBuffer = false;

            GetControlsValuesFromCache();
            LoadDataToControlsFromCache();

        }

        public void updateData()
        {
          LoadControlParams();
          LoadDataToControlsFromCache();
        }

      private void LoadDataToControlsFromCache()
        {
            if (CacheParameters.AllParameters)
            foreach (BaseLayoutItem item in layoutControl.Items)
            {
                if (item is LayoutControlItem)
                {
                    Control control = (item as LayoutControlItem).Control;
                    if ((control == null) || (control.Tag == null))
                        continue;


                    switch (control.GetType().Name)
                    {
                        case "CheckedComboBoxEdit":
                        {
                            var cB = (CheckedComboBoxEdit) control;

                            if (CacheAllControls != null)
                                if (CacheAllControls.Controls != null)
                                    foreach (Cache cache in CacheAllControls.Controls)
                                    {
                                        if (cB.Name == cache.Control)
                                        {
                                            cB.SetEditValue(cache.Values);
                                        }
                                    }
                        }
                            break;
                        case "LookUpEdit":
                        {
                            var lE = (LookUpEdit) control;

                            if (CacheAllControls != null)
                                if (CacheAllControls.Controls != null)
                                    foreach (Cache cache in CacheAllControls.Controls)
                                    {
                                        if (lE.Name == cache.Control)
                                        {
                                            lE.EditValue = cache.Values;
                                        }
                                    }
                        }
                            break;
                        case "RadioGroup":
                        {
                            var rG = (RadioGroup) control;


                            if (CacheAllControls != null)
                                if (CacheAllControls.Controls != null)
                                    foreach (Cache cache in CacheAllControls.Controls)
                                    {
                                        if (rG.Name == cache.Control)
                                        {
                                            int index;
                                            bool result = Int32.TryParse(cache.Values,out index);
                                            if (result)
                                            rG.SelectedIndex = Convert.ToInt32(index);
                                        }
                                    }
                        }
                            break;
                        case "TreeList":
                        {
                            var tL = (TreeList) control;
                            if (CacheAllControls != null)
                                if (CacheAllControls.Controls != null)
                                    foreach (Cache cache in CacheAllControls.Controls)
                                    {
                                        tL.BeginUpdate();
                                        try
                                        {
                                            if (tL.Name == cache.Control)
                                                if (!string.IsNullOrEmpty(cache.Values))
                                                {
                                                    string[] data = cache.Values.Split(new[] { ", " }, StringSplitOptions.None);
                                                    for (int i = 0; i < tL.Nodes.Count; i++)
                                                    {
                                                        if (useLikeForEKT)
                                                            SetChildNodesBySapId(tL.Nodes[i], data);
                                                        else
                                                            SetChildNodes(tL.Nodes[i], data);
                                                    }
                                                }
                                        }
                                        finally
                                        {
                                            tL.EndUpdate();
                                            tL.CollapseAll();
                                        }
                                    }
                        }
                            break;
                        case "ComboBoxEdit":
                        {
                            var cB = (ComboBoxEdit) control;



                            if (CacheAllControls != null)
                                if (CacheAllControls.Controls != null)
                                    foreach (Cache cache in CacheAllControls.Controls)
                                    {
                                        if (cB.Name == cache.Control)
                                        {
                                            cB.SelectedIndex = Convert.ToInt32(cache.Values);
                                        }
                                    }
                        }
                            break;
                        case "ButtonEdit":
                        {
                            var textEdit = (ButtonEdit) control;

                            if (CacheAllControls != null)
                                if (CacheAllControls.Controls != null)
                                    foreach (Cache cache in CacheAllControls.Controls)
                                    {
                                        if (textEdit.Name == cache.Control)
                                        {
                                            textEdit.EditValue = cache.Values;
                                        }
                                    }
                        }
                            break;
                        case "TextEdit":
                        {
                            var textEdit = (TextEdit) control;

                            if (CacheAllControls != null)
                                if (CacheAllControls.Controls != null)
                                    foreach (Cache cache in CacheAllControls.Controls)
                                    {
                                        if (textEdit.Name == cache.Control)
                                        {
                                            textEdit.EditValue = cache.Values;
                                        }
                                    }
                        }
                            break;
                        case "CheckEdit":
                        {

                            var checkEdit = (CheckEdit) control;


                            if (CacheAllControls != null)
                                if (CacheAllControls.Controls != null)
                                    foreach (Cache cache in CacheAllControls.Controls)
                                    {
                                        if (checkEdit.Name == cache.Control)
                                        {
                                            checkEdit.Checked = Convert.ToBoolean(cache.Values);
                                        }
                                    }

                        }
                            break;
                    }
                }

            }

            if(CacheParameters.Sign)
            if (CacheAllControls != null)
                if (CacheAllControls.Parameters !=null)
                if (CacheAllControls.Parameters.Count > 0)
                    if (SignFilter != null) SignFilter.LoadFromCache(CacheAllControls.Parameters,CacheParameters.SignValue);
        }

        #region Глобальные переменные и загрузки значений контролов

        private Guid _masterDataRequestGuid = Guid.Empty;
        public Boolean IsClearData { get; set; }
        public Boolean IsFromBuffer { get; set; }
        public Boolean IsChecked { get; set; }

        /// <summary>
        /// Фильтр отобраных данных по товару
        /// </summary>
        public string goodsRequest
        {
            get;
            set;
        }

        /// <summary>
        /// Фильтр отобраных данных по товару
        /// </summary>
        public Filter goodsFilter
        {
            get;
            set;
        }

        /// <summary>
        /// Фильтр отобранных данных (массив)
        /// </summary>
        public int[] GoodsArray { get; private set; }

        /// <summary>
        /// Операция в корой используется фильтр для определения разрешений пользователя
        /// </summary>
        public string OperationName
        {
            get;
            set;
        }

        public Dictionary<string, string> HintTemplates = new Dictionary<string, string>
        {
            {Fields.lagerId, "Артикул: {0}"},
            {"lagerSet", "Набор артикулов ({0})"},
            {"lagerSetExclude", "Все артикулы кроме ({0})"},
            {"lagerSetLarge", "Набор артикулов {0} шт."},
            {"lagerSetLargeExclude", "Все артикулы кроме {0} шт."},
            {Fields.barcode, "Ш/К содержит '{0}'"},
            {Fields.lagerName, "Название содержит '{0}'"},
            {Fields.lagerUnit, "Ед.Изм: {0}"},
            {Fields.departmentId, "Отдел: {0}"},
            {Fields.macroId, "Макрогруппа {0}"},
            {Fields.gentlemanSetId, "Дж.набор: {0}"},
            {Fields.purchaseGroupSapId, "Закуп. группа: {0}"},
            {Fields.brandId, "Брэнд: {0}"},
            {Fields.lagerTypeId, "Тип товара: {0}"},
            {Fields.lagerPrivateLabel, "СТМ: {0}"},
            {Fields.lagerOwnImport, "Свой импорт: {0}"},
            {Fields.lagerClassifierId, "Группа классиф: {0}"},
            {Fields.lagerIndicativeSoc, "Индикатив соц.: {0}"},
            {Fields.lagerIndicativeAlc, "Индикатив алк.: {0}"}
        };

        /// <summary>
        /// Загружаем параметры контролов фильтра и ключевые значения
        /// </summary>
        public void LoadControlParams()
        {
            ELagerId.Tag = Fields.lagerId;
            EBarcode.Tag = Fields.barcode;
            EName.Tag = Fields.lagerName;
            EEdIzm.Tag = Fields.lagerUnit;
            cBDepartment.Properties.ValueMember = Fields.departmentId;
            cBDepartment.Properties.DisplayMember = Fields.departmentName;
            cBDepartment.Tag = Fields.departmentId;
            cBMacrogroup.Properties.ValueMember = Fields.macroId;
            cBMacrogroup.Properties.DisplayMember = Fields.macroName;
            cBMacrogroup.Tag = Fields.macroId;

            cBPurchaseGroup.Properties.ValueMember = Fields.purchaseGroupSapId;
            cBPurchaseGroup.Properties.DisplayMember = Fields.purchaseGroupName;
            cBPurchaseGroup.Tag = Fields.purchaseGroupSapId;
            lEBrand.Properties.ValueMember = Fields.brandId;
            lEBrand.Properties.DisplayMember = Fields.brandName;
            lEBrand.Tag = Fields.brandId;
            cBLagerType.Properties.ValueMember = Fields.lagerTypeId;
            cBLagerType.Properties.DisplayMember = Fields.lagerTypeName;
            cBLagerType.Tag = Fields.lagerTypeId;
            tLSapClassifier.KeyFieldName = Fields.classifierId;
            tLSapClassifier.ParentFieldName = Fields.classifierParentId;
            tLSapClassifier.Tag = Fields.lagerClassifierId;
            gCLagerId.FieldName = Fields.lagerId;
            gCLagerName.FieldName = Fields.lagerName;
            gCLagerUnit.FieldName = Fields.lagerUnit;
            gCBarcode.FieldName = Fields.barcode;
            gCChecked.FieldName = "AplyGoods";
            gCLagerUnitTypeName.FieldName = Fields.lagerUnitTypeName;
            gCLagerUnitQuantity.FieldName = Fields.lagerUnitQuantity;
            goodsGrid.Tag = Fields.lagerId;

            сBOwnImport.Tag = Fields.lagerOwnImport;
            сBOwnImport.Properties.Items.Add(new ComboBoxItem("Без учета"));
            сBOwnImport.Properties.Items.Add(new ComboBoxItem("Только"));
            сBOwnImport.Properties.Items.Add(new ComboBoxItem("Исключая"));
            сBOwnImport.SelectedIndex = 0;
            сBSTM.Tag = Fields.lagerPrivateLabel;
            сBSTM.Properties.Items.Add(new ComboBoxItem("Без учета"));
            сBSTM.Properties.Items.Add(new ComboBoxItem("Только"));
            сBSTM.Properties.Items.Add(new ComboBoxItem("Исключая"));
            сBSTM.SelectedIndex = 0;

            ceIndicSoc.Tag = Fields.lagerIndicativeSoc;
            ceIndicAlco.Tag = Fields.lagerIndicativeAlc;

            rgVAT.Tag = Fields.VAT;
            rgVAT.SelectedIndex = 0;
        }

        #endregion

        #region Загрузки справочников

        /// <summary>
        /// Вызывается при завершении первоначальной справочников
        /// </summary>
        protected event Action LoadDictionaryFinishedEvent;

        /// <summary>
        /// Установка ChaseControl на все контролы при отборе справочников
        /// </summary>
        private void AddChaseControlOnFilter()
        {

            layoutControl.Items.ConvertToTypedList().ForEach(delegate(BaseLayoutItem item)
            {
                if (item is LayoutControlItem)
                {
                    if (((item as LayoutControlItem).Control is CheckedComboBoxEdit) ||
                        ((item as LayoutControlItem).Control is RadioGroup) ||
                        ((item as LayoutControlItem).Control is TextEdit) ||
                        ((item as LayoutControlItem).Control is LookUpEdit) ||
                        ((item as LayoutControlItem).Control is TreeList) ||
                        ((item as LayoutControlItem).Control is CheckEdit))
                    {
                        (item as LayoutControlItem).Control.Enabled = false;
                        ChaseControl.AddToControl((item as LayoutControlItem).Control, "Загрузка...");
                    }
                }
            });
            bOk.Enabled = false;
            TGoodsFilterGrid.PageEnabled = false;
        }

        /// <summary>
        /// Удаление ChaseControl на все контролы при отборе справочников
        /// </summary>
        private void RemoveChaseControlOnFilter()
        {
            layoutControl.Items.ConvertToTypedList().ForEach(delegate(BaseLayoutItem item)
            {
                if (item is LayoutControlItem)
                {
                    if (((item as LayoutControlItem).Control is CheckedComboBoxEdit) ||
                        ((item as LayoutControlItem).Control is RadioGroup) ||
                        ((item as LayoutControlItem).Control is TextEdit) ||
                        ((item as LayoutControlItem).Control is LookUpEdit) ||
                        ((item as LayoutControlItem).Control is ComboBoxEdit) ||
                        ((item as LayoutControlItem).Control is TreeList) ||
                        ((item as LayoutControlItem).Control is CheckEdit))
                    {
                        (item as LayoutControlItem).Control.Enabled = true;
                        ChaseControl.RemoveFromControl((item as LayoutControlItem).Control);
                    }
                }
            });

            bOk.Enabled = true;
            TGoodsFilterGrid.PageEnabled = true;
        }

        /// <summary>
        /// Загрузка справочников из MasterData для поиска товаров.
        /// </summary>
        public void GetGoodsDataDictionary()
        {
            if (MasterDataSet == null)
            {

                try
                {
                    var dimensions = new List<Dimension>
                    {
                        new Dimension
                        {
                            columns =
                                new[] {Fields.macroId, Fields.macroName},
                            name = Dims.DimLagers,
                            operationNames = new[] {OperationName},
                            resultName = "MacroGroups",
                            orderBy = new[] {new OrderBy {Value = Fields.macroName}}
                        },
                        new Dimension
                        {
                            columns =
                                new[]
                                {Fields.departmentId, Fields.departmentName},
                            name = Dims.DimLagers,
                            operationNames = new[] {OperationName},
                            resultName = "Departments",
                            orderBy = new[] {new OrderBy {Value = Fields.departmentName}}
                        },
                        new Dimension
                        {
                            columns =
                                new[]
                                {Fields.lagerTypeId, Fields.lagerTypeName},
                            name = Dims.DimLagers,
                            operationNames = new[] {OperationName},
                            resultName = "LagerTypes",
                            orderBy = new[] {new OrderBy {Value = Fields.lagerTypeName}}
                        },
                        new Dimension
                        {
                            columns =
                                new[] {Fields.brandId, Fields.brandName},
                            name = Dims.DimLagers,
                            operationNames = new[] {OperationName},
                            resultName = "Brands",
                            orderBy = new[] {new OrderBy {Value = Fields.brandName}}
                        },
                        new Dimension
                        {
                            columns =
                                new[]
                                {
                                    Fields.purchaseGroupSapId,
                                    Fields.purchaseGroupName
                                },
                            name = Dims.DimLagers,
                            operationNames = new[] {OperationName},
                            resultName = "PurchaseGroups",
                            expression =
                                String.Format("ISNULL({0}, '') != ''", Fields.purchaseGroupSapId),
                            orderBy = new[] {new OrderBy {Value = Fields.purchaseGroupName}}
                        },
                        new Dimension
                        {
                            columns =
                                new[]
                                {
                                    Fields.classifierId,
                                    Fields.classifierParentId,
                                    Fields.classifierSAPID,
                                    Fields.classifierName
                                },
                            name = Dims.DimLagerSapClassifier,
                            operationNames = new[] {OperationName},
                            resultName = "LagerSapClassifier",
                            orderBy = new[] {new OrderBy {Value = Fields.classifierName}}
                        },
                        new Dimension
                        {
                            columns =
                                new[] {"sku.Parameters.parameterId", "sku.Parameters.parameterName", "sku.Parameters.parameterNameEN", "sku.Parameters.listId", "sku.Parameters.dataTypeId"},
                            name = Dims.DimParameters,
                            resultName = "Parameters"
                        },
                        new Dimension
                        {
                            columns = new[] {"sku.Parameters.listId", "sku.Lists.listValueId", "sku.Lists.listValue"},
                            name = Dims.DimParameterValues,
                            resultName = "ParameterValues"
                        }
                    };


                    _masterDataRequestGuid = FZCoreProxy.GetMasterDataAsyncStreamed(null, GetGoodsDataDictionaryCallBack,
                        new MasterDataRequest
                        {
                            dim = dimensions.ToArray()
                        });
                }
                catch (Exception ex)
                {
                    MB.error(ex);
                    DialogResult = DialogResult.Cancel;
                }
            }
            else
            {
                SetDataToControls();
            }
        }

        /// <summary>
        /// Загрузка справочников из MasterData для поиска товаров. CallBack
        /// </summary>
        private void GetGoodsDataDictionaryCallBack(IDefaultContract o, object d)
        {
            try
            {
                var c = o as DefaultMessageContract;
                if (c != null)
                {
                    var reader = c.GetDataReader();
                    if (reader.IsClosed)
                        throw new FZException(ErrorCodes.LOAD_DATA_ERROR, "reader is closed");

                    MasterDataSet = new DataSet("md");
                    MasterDataSet.Tables.Add("MacroGroups");
                    MasterDataSet.Tables.Add("Departments");
                    MasterDataSet.Tables.Add("LagerTypes");
                    MasterDataSet.Tables.Add("Brands");
                    MasterDataSet.Tables.Add("PurchaseGroups");
                    MasterDataSet.Tables.Add("LagerSapClassifier");
                    MasterDataSet.Tables.Add("Parameters");
                    MasterDataSet.Tables.Add("ParameterValues");
                    if ((IsDisposed) && (o.errorCode == ErrorCodes.CANCELLED))
                        return;

                    if (o.errorCode != ErrorCodes.OK)
                        throw new Exception(o.errorString);

                    MasterDataSet.Load(reader, LoadOption.OverwriteChanges, "MacroGroups", "Departments", "LagerTypes", "Brands", "PurchaseGroups", "LagerSapClassifier", "Parameters", "ParameterValues");
                }

                var col = new DataColumn
                {
                    DataType = typeof(string),
                    Expression = MasterDataGoodsFilter.Fields.classifierSAPID + " + ' ' + " +
                                 MasterDataGoodsFilter.Fields.classifierName
                };
                MasterDataSet.Tables["LagerSapClassifier"].Columns.Add(col);

                SetDataToControls();
            }
            catch (Exception ex)
            {
                MB.error(ex);
                RemoveChaseControlOnFilter();
            }
            finally
            {
                if (!IsDisposed)
                    _masterDataRequestGuid = Guid.Empty;
            }
        }


        private void SetDataToControls()
        {

            if (IsDisposed)
            {
                return;
            }

            //Установка DataSet в контролы фильтрации
            cBDepartment.Properties.DataSource = MasterDataSet.Tables["Departments"];
            cBMacrogroup.Properties.DataSource = MasterDataSet.Tables["MacroGroups"];
            cBPurchaseGroup.Properties.DataSource = MasterDataSet.Tables["PurchaseGroups"];
            lEBrand.Properties.DataSource = MasterDataSet.Tables["Brands"];
            cBLagerType.Properties.DataSource = MasterDataSet.Tables["LagerTypes"];

            tLSapClassifier.DataSource = MasterDataSet.Tables["LagerSapClassifier"].AsDataView();

            var ds = new DataSet();
            ds.Tables.Add(MasterDataSet.Tables["Parameters"].Copy());
            ds.Tables.Add(MasterDataSet.Tables["ParameterValues"].Copy());
            SignFilter = new Signs(ds);

            LayoutControlItem item = ControlGroupSign.AddItem();
            item.Control = SignFilter;
            ControlGroupSign.TextVisible = false;
            item.TextVisible = false;

            _filterHint = new List<string>();


            tLSapClassifier.ForceInitialize();

            if (LoadDictionaryFinishedEvent != null)
                LoadDictionaryFinishedEvent();

        }

        #endregion

        #region Обработчики фильтров

        /// <summary>
        /// Создание FilterSet по выбраным параметрам фильтрации
        /// </summary>
        private Filter AssembleGoodsFilter()
        {
            var li = new List<int>();
            //String requestPart;
            var filterSet = new Filter(GroupOperatorType.And);
            _filterHint.Clear();
            string hintText = String.Empty;
            string fieldTag = String.Empty;

            foreach (BaseLayoutItem item in layoutControl.Items)
            {
                if (item is LayoutControlItem)
                {
                    Control control = (item as LayoutControlItem).Control;
                    if ((control == null) || (control.Tag == null))
                        continue;

                    fieldTag = control.Tag.ToString();



                    switch (control.GetType().Name)
                    {
                        case "CheckedComboBoxEdit":
                        {
                            var cB = (CheckedComboBoxEdit) control;

                            if (
                                !String.IsNullOrEmpty(
                                    cB.Properties.GetCheckedItems().ToString()))
                            {

                                if ((cB.Properties.DataSource as DataTable).Columns[fieldTag].DataType.ToString()
                                    .Contains("Int"))
                                {
                                    filterSet.Add(
                                        new Filter(String.Format("[{0}] in ({1})", fieldTag,
                                            cB.Properties.GetCheckedItems())));
                                }
                                else
                                {
                                    filterSet.Add(new InOperator(fieldTag,
                                        ((string) cB.Properties.GetCheckedItems()).Split(new[] {", "},
                                            StringSplitOptions.RemoveEmptyEntries)));
                                }
                                hintText = cB.Properties.GetDisplayText(null);
                            }
                        }
                            break;
                        case "LookUpEdit":
                        {
                            var lE = (LookUpEdit) control;

                            if (lE.EditValue != null)
                            {
                                filterSet.Add(
                                    new Filter(String.Format("[{0}] in ({1})", fieldTag,
                                        lE.GetColumnValue(lE.Properties.Columns[fieldTag]))));
                                hintText = lE.Properties.GetDisplayText(lE.EditValue).Trim();
                            }
                        }
                            break;
                        case "RadioGroup":
                        {
                            var rG = (RadioGroup) control;
                            if (rG.Equals(rgVAT))
                            {
                                if (rgVAT.SelectedIndex > 0)
                                    filterSet.Add(
                                        new Filter(String.Format("isnull([{0}], 0) {1}", fieldTag, rG.EditValue)));
                            }
                            else
                                filterSet.Add(
                                    new Filter(rG.Properties.Items[rG.SelectedIndex].Value != null
                                        ? rG.Properties.Items[rG.SelectedIndex].Value.ToString()
                                        : null));
                        }
                            break;
                        case "TreeList":
                        {
                            var tL = (TreeList) control;
                            var dataList = new List<int>();
                            var dataListNames = new List<string>();
                            var dataListTopSapId = new List<string>();
                            var dataListTopNames = new List<string>();
                            for (int i = 0; i < tL.Nodes.Count; i++)
                            {
                                if (useLikeForEKT)
                                {
                                    dataListTopSapId.AddRange(GetTopChildNodes(tL.Nodes[i], Fields.classifierSAPID));
                                    dataListTopNames.AddRange(GetTopChildNodes(tL.Nodes[i], Fields.classifierName));
                                }
                                else
                                {
                                    dataList.AddRange(GetChildNodes(tL.Nodes[i]));
                                    dataListNames.AddRange(GetChildNodesNames(tL.Nodes[i]));
                                }
                            }
                            if (dataList.Count != 0 || dataListTopSapId.Count != 0)
                            {
                                Filter like = new Filter();
                                InOperator io = new InOperator();
                                if (useLikeForEKT)
                                {
                                    like = new Filter(GroupOperatorType.Or);
                                    foreach (string sapId in dataListTopSapId)
                                        like.Add(new BinaryOperator(Fields.lagerClassifierSAPId, sapId + "%", BinaryOperatorType.Like));
                                    hintText = String.Join(", ", dataListTopNames.ToArray());
                                }
                                else
                                {
                                    io = new InOperator(fieldTag, dataList);
                                    hintText = String.Join(", ", dataListNames.ToArray());
                                }
                                if (!cEExcludeSapClassifier.Checked)
                                {
                                    if (useLikeForEKT)
                                        filterSet.Add(like);
                                    else
                                        filterSet.Add(io);
                                }
                                else
                                {
                                    if (useLikeForEKT)
                                        filterSet.Add(new NotOperator(like));
                                    else
                                        filterSet.Add(new NotOperator(io));
                                    hintText = "все, кроме " + hintText;
                                }
                            }
                        }
                            break;
                        case "ComboBoxEdit":
                        {
                            var cB = (ComboBoxEdit) control;

                            if (cB.SelectedIndex <= 0)
                                continue;

                            switch (cB.SelectedIndex)
                            {
                                case 1:
                                    filterSet.Add(new Filter(String.Format("[{0}] = 1", fieldTag)));
                                    break;
                                case 2:
                                    filterSet.Add(new Filter(String.Format("[{0}] = 0", fieldTag)));
                                    break;

                            }

                            hintText = cB.Properties.Items[cB.SelectedIndex].ToString();
                        }
                            break;
                        case "ButtonEdit":
                        {
                            var textEdit = (TextEdit) control;

                            if (!String.IsNullOrEmpty(textEdit.Text))
                            {
                                if (fieldTag == MasterDataGoodsFilter.Fields.lagerId)
                                {
                                    try
                                    {
                                        if (textEdit.Text == @"набор")
                                        {
                                            li.AddRange(GoodsArray);
                                            hintText = "(" +
                                                       String.Join(", ",
                                                           li.Select(i => i.ToString(CultureInfo.InvariantCulture))
                                                               .ToArray()) + ")";
                                        }
                                        else
                                        {
                                            li.Add(int.Parse(textEdit.Text));
                                            hintText = textEdit.Text;
                                        }

                                        var io = new InOperator(fieldTag, li);
                                        if (!cEExcludeLagerId.Checked)
                                        {
                                            filterSet.Add(io);
                                        }
                                        else
                                        {
                                            filterSet.Add(new NotOperator(io));
                                            hintText = "все, кроме " + hintText;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex is FormatException)
                                            MB.error("Идентификатор артикула не является числом");
                                        else
                                            MB.error(ex);
                                    }
                                }
                                else if (fieldTag == MasterDataGoodsFilter.Fields.lagerUnit)
                                {
                                    if (cBEdIzmCompareType.SelectedIndex == 3) // не равно
                                    {
                                        filterSet.Add(
                                            new Filter(String.Format("[{0}] <> '{1}'", fieldTag,
                                                Sql.SafeSqlLiteral(textEdit.Text))));
                                        hintText = "не " + textEdit.Text;
                                    }
                                    else if (cBEdIzmCompareType.SelectedIndex == 2) // равно
                                    {
                                        filterSet.Add(
                                            new Filter(String.Format("[{0}] = '{1}'", fieldTag,
                                                Sql.SafeSqlLiteral(textEdit.Text))));
                                        hintText = textEdit.Text;
                                    }
                                    else if (cBEdIzmCompareType.SelectedIndex == 1) // не содержит
                                    {
                                        filterSet.Add(
                                            new Filter(String.Format("[{0}] not like '%{1}%'", fieldTag,
                                                Sql.SafeSqlLiteral(textEdit.Text))));
                                        hintText = String.Format("не содержит '{0}'", textEdit.Text);
                                    }
                                    else // содержит
                                    {
                                        filterSet.Add(
                                            new Filter(String.Format("[{0}] like '%{1}%'", fieldTag,
                                                Sql.SafeSqlLiteral(textEdit.Text))));
                                        hintText = String.Format("содержит '{0}'", textEdit.Text);
                                    }
                                }
                                else
                                {
                                    filterSet.Add(
                                        new Filter(String.Format("[{0}] like '%{1}%'", fieldTag,
                                            Sql.SafeSqlLiteral(textEdit.Text))));
                                    hintText = textEdit.Text;
                                }
                            }
                        }
                            break;
                        case "TextEdit":
                        {
                            var textEdit = (TextEdit) control;

                            if (!String.IsNullOrEmpty(textEdit.Text))
                            {
                                if (fieldTag == MasterDataGoodsFilter.Fields.lagerId)
                                {
                                    try
                                    {
                                        if (textEdit.Text == @"набор")
                                        {
                                            li.AddRange(GoodsArray);
                                            hintText = "(" +
                                                       String.Join(", ",
                                                           li.Select(i => i.ToString(CultureInfo.InvariantCulture))
                                                               .ToArray()) + ")";
                                        }
                                        else
                                        {
                                            li.Add(int.Parse(textEdit.Text));
                                            hintText = textEdit.Text;
                                        }

                                        var io = new InOperator(fieldTag, li);
                                        if (!cEExcludeLagerId.Checked)
                                        {
                                            filterSet.Add(io);
                                        }
                                        else
                                        {
                                            filterSet.Add(new NotOperator(io));
                                            hintText = "все, кроме " + hintText;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex is FormatException)
                                            MB.error("Идентификатор артикула не является числом");
                                        else
                                            MB.error(ex);
                                    }
                                }
                                else if (fieldTag == MasterDataGoodsFilter.Fields.lagerUnit)
                                {
                                    if (cBEdIzmCompareType.SelectedIndex == 3) // не равно
                                    {
                                        filterSet.Add(
                                            new Filter(String.Format("[{0}] <> '{1}'", fieldTag,
                                                Sql.SafeSqlLiteral(textEdit.Text))));
                                        hintText = "не " + textEdit.Text;
                                    }
                                    else if (cBEdIzmCompareType.SelectedIndex == 2) // равно
                                    {
                                        filterSet.Add(
                                            new Filter(String.Format("[{0}] = '{1}'", fieldTag,
                                                Sql.SafeSqlLiteral(textEdit.Text))));
                                        hintText = textEdit.Text;
                                    }
                                    else if (cBEdIzmCompareType.SelectedIndex == 1) // не содержит
                                    {
                                        filterSet.Add(
                                            new Filter(String.Format("[{0}] not like '%{1}%'", fieldTag,
                                                Sql.SafeSqlLiteral(textEdit.Text))));
                                        hintText = String.Format("не содержит '{0}'", textEdit.Text);
                                    }
                                    else // содержит
                                    {
                                        filterSet.Add(
                                            new Filter(String.Format("[{0}] like '%{1}%'", fieldTag,
                                                Sql.SafeSqlLiteral(textEdit.Text))));
                                        hintText = String.Format("содержит '{0}'", textEdit.Text);
                                    }
                                }
                                else
                                {
                                    filterSet.Add(
                                        new Filter(String.Format("[{0}] like '%{1}%'", fieldTag,
                                            Sql.SafeSqlLiteral(textEdit.Text))));
                                    hintText = textEdit.Text;
                                }
                            }
                        }
                            break;
                        case "CheckEdit":
                        {
                            if ((control as CheckEdit).Checked)
                            {
                                filterSet.Add(new Filter(String.Format("[{0}] = 1", fieldTag)));
                            }
                        }
                            break;
                    }
                }
                // добавляем хинт по полю если есть шаблон для этого поля и текст хинта не пустой          
                if ((!String.IsNullOrEmpty(hintText)) && (HintTemplates.ContainsKey(fieldTag)))
                    _filterHint.Add(String.Format(HintTemplates[fieldTag], hintText));
            }
            if (Parameters != null && Parameters.Count > 0)
                foreach (var param in Parameters)
                {
                    switch (param.DataTypeId)
                    {
                        case (int) ParameterDataType.Dictionary:
                        {
                            if (String.IsNullOrEmpty(param.SignValues))
                            {
                                if (param.Equal == "IN")
                                    filterSet.Add(
                                        new Filter(String.Format("[sku.ListValues.{0}] is null", param.SignName)));
                                if (param.Equal == "NOT IN")
                                    filterSet.Add(
                                        new Filter(String.Format("[sku.ListValues.{0}] is not null", param.SignName)));
                            }
                            else
                            {
                                string[] parameters =
                                    param.SignValues.Split(',');

                                string parametersString = string.Empty;
                                string result = string.Empty;
                                foreach (
                                    string parameter in
                                        parameters.Where(parameter => parameter != "У артикула нет признака."))
                                {
                                    result = result + String.Format("'{0}',", parameter.TrimStart(' '));
                                    parametersString =
                                        result
                                            .TrimEnd(',');
                                }

                                bool needNull = false;

                                foreach (string parameter in parameters)
                                {
                                    if (parameter == "У артикула нет признака.")
                                    {needNull = true;
                                    }
                                }
                                if (needNull)
                                {
                                    if (parameters.Any())
                                    {
                                        string filter = String.Format(
                                            "[sku.ListValues.{0}] in ({1}) or [sku.ListValues.{0}] is null",
                                            param.SignName,
                                            parametersString);

                                        filterSet.Add(
                                            new Filter(filter));
                                    }
                                    else

                                        filterSet.Add(
                                            new Filter(String.Format("[sku.ListValues.{0}] is null", param.SignName)));

                                }
                                else
                                {
                                    if (parameters.Any())
                                    {
                                       string filter = String.Format(
                                            "[sku.ListValues.{0}] in ({1})",
                                            param.SignName,
                                            parametersString);
                                        
                                        filterSet.Add(
                                            new Filter(filter));
                                    }
                                    else
                                        filterSet.Add(
                                            new Filter(String.Format("[sku.ListValues.{0}] = '{1}'", param.SignName,
                                                param.SignValues)));

                                }
                            }

                        }


                            break;
                        case (int) ParameterDataType.Numbers:
                        {
                            if (String.IsNullOrEmpty(param.SignValues))
                            {
                                if (param.Equal == "IN")
                                    filterSet.Add(
                                        new Filter(String.Format("[sku.ListValues.{0}] is null", param.SignName)));
                                if (param.Equal == "NOT IN")
                                    filterSet.Add(
                                        new Filter(String.Format("[sku.ListValues.{0}] is not null", param.SignName)));
                            }
                            else
                                filterSet.Add(
                                    new Filter(String.Format("[sku.ListValues.{0}] = '{1}'", param.SignName,
                                        Sql.SafeSqlLiteral(param.SignValues))));
                        }
                            break;
                        case (int) ParameterDataType.Text:
                        {
                            if (String.IsNullOrEmpty(param.SignValues))
                            {
                                if (param.Equal == "IN")
                                    filterSet.Add(
                                        new Filter(String.Format("[sku.ListValues.{0}] is null", param.SignName)));
                                if (param.Equal == "NOT IN")
                                    filterSet.Add(
                                        new Filter(String.Format("[sku.ListValues.{0}] is not null", param.SignName)));
                            }
                            else
                                filterSet.Add(
                                    new Filter(String.Format("sku.ListValues.[{0}] like '%{1}%'", param.SignName,
                                        Sql.SafeSqlLiteral(param.SignValues))));
                        }
                            break;
                    }
                }

            GoodsArray = li.ToArray();
            goodsFilter = filterSet;
            return filterSet;
        }

        /// <summary>
        /// Механизм кеширования
        /// </summary>
        private void CacheControl()
        {
            if (CacheAllControls != null)
            {
                CacheAllControls = null;
                CacheAllControls = new CacheControls {Controls = new List<Cache>()};
            }
            else
            {
                CacheAllControls = new CacheControls { Controls = new List<Cache>() };
            }


            foreach (BaseLayoutItem item in layoutControl.Items)
            {
                if (item is LayoutControlItem)
                {
                    Control control = (item as LayoutControlItem).Control;
                    if ((control == null) || (control.Tag == null))
                        continue;


                    switch (control.GetType().Name)
                    {
                        case "CheckedComboBoxEdit":
                            {
                                var cB = (CheckedComboBoxEdit)control;

                                if (
                                    !String.IsNullOrEmpty(
                                        cB.Properties.GetCheckedItems().ToString()))
                                {
                                    var cache = new Cache
                                    {
                                        Control = cB.Name,
                                        Values = cB.Properties.GetCheckedItems().ToString()
                                    };

                                    if (CacheAllControls != null) CacheAllControls.Controls.Add(cache);
                                }
                            }
                            break;
                        case "LookUpEdit":
                            {
                                var lE = (LookUpEdit)control;

                                if (lE.EditValue != null)
                                {
                                    var cache = new Cache
                                    {
                                        Control = lE.Name,
                                        Values = lE.EditValue.ToString()
                                    };

                                    if (CacheAllControls != null) CacheAllControls.Controls.Add(cache);
                                }
                            }
                            break;
                        case "RadioGroup":
                            {
                                var rG = (RadioGroup)control;

                                var cache = new Cache();

                                if (rG.Equals(rgVAT))
                                {
                                    if (rgVAT.SelectedIndex > 0)
                                    {
                                        if (rG.EditValue != null)
                                        {
                                            cache.Control = rG.Name;
                                            cache.Values = rG.SelectedIndex.ToString(CultureInfo.InvariantCulture);
                                        }
                                    }
                                }
                                else if (rG.Properties.Items[rG.SelectedIndex].Value != null)
                                {
                                cache.Control = rG.Name;
                                    cache.Values = rG.Properties.Items[rG.SelectedIndex].Value.ToString();
                                }

                                if (CacheAllControls != null) CacheAllControls.Controls.Add(cache);
                            }
                            break;
                        case "TreeList":
                        {
                            var tL = (TreeList) control;
                            var dataList = new List<int>();
                            var dataListNames = new List<string>();
                            for (int i = 0; i < tL.Nodes.Count; i++)
                            {
                                dataList.AddRange(GetChildNodes(tL.Nodes[i]));
                                dataListNames.AddRange(GetChildNodesNames(tL.Nodes[i]));
                            }


                            var cache = new Cache
                            {
                                Control = tL.Name,
                                Values = String.Join(", ", dataListNames.ToArray())
                            };

                            if (CacheAllControls != null) CacheAllControls.Controls.Add(cache);

                        }
                            break;
                        case "ComboBoxEdit":
                            {
                                var cB = (ComboBoxEdit)control;

                                var cache = new Cache {Control = cB.Name, Values = cB.SelectedIndex.ToString(CultureInfo.InvariantCulture)};

                                if (CacheAllControls != null) CacheAllControls.Controls.Add(cache);
                            }
                            break;
                        case "ButtonEdit":
                            {
                                var textEdit = (TextEdit)control;
                                if (textEdit.EditValue != null)
                                {
                                    var cache = new Cache { Control = textEdit.Name, Values = textEdit.EditValue.ToString() };

                                    if (CacheAllControls != null) CacheAllControls.Controls.Add(cache);
                                }
                            }
                            break;
                        case "TextEdit":
                            {
                                var textEdit = (TextEdit)control;
                                if (textEdit.EditValue != null)
                                {
                                    var cache = new Cache
                                    {
                                        Control = textEdit.Name,
                                        Values = textEdit.EditValue.ToString()
                                    };

                                    if (CacheAllControls != null) CacheAllControls.Controls.Add(cache);
                                }
                            }
                            break;
                        case "CheckEdit":
                            {

                                var checkEdit = (CheckEdit)control;

                                var cache = new Cache { Control = checkEdit.Name, Values = checkEdit.Checked.ToString() };

                                if (CacheAllControls != null) CacheAllControls.Controls.Add(cache);
                            }
                            break;
                    }
                }

            }

            if (CacheAllControls != null)
            {
                CacheAllControls.Parameters = Parameters;

                CacheHelper.GetHelper("FilterProperties").Put(FilterSettingsKey, Serialization.Serialize(CacheAllControls));
            }
        }


        private void GetControlsValuesFromCache()
        {

            CacheAllControls = null;
            string settingsString = CacheHelper.GetHelper("FilterProperties").Get(FilterSettingsKey);

            {
                try
                {
                    CacheAllControls = (String.IsNullOrEmpty(settingsString)) ?
                       new CacheControls()
                       :
                       Serialization.Deserialize<CacheControls>(settingsString);

                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Разбор FilterSet по переданой строке
        /// </summary>
        public Filter AssembleGoodsFilterFromClipboard()
        {
            // Процедура создает фильтр на основе данных из буфера обмена
            var filterSet = new Filter(GroupOperatorType.And);
            try
            {
                bool isLagers = (rgImportFld.Enabled) && (rgImportFld.SelectedIndex == 2);
                // Забираем список артикулов из буфера обмена
                IDataObject iData = Clipboard.GetDataObject();

                if (iData != null && iData.GetDataPresent(DataFormats.Text))
                {
                    var s = (string) iData.GetData(typeof (String));
                    var dataList = new List<object>();

                    string[] items = s.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);


                    for (int i = 0; i < (items.Length <= 50000 ? items.Length : 50000); i++)
                    {
                        if (isLagers)
                        {
                            int d;
                            if (int.TryParse(items[i], out d))
                                dataList.Add(int.Parse(items[i]));
                        }
                        else
                            dataList.Add(items[i]);
                    }
                    
                    // Проверяем кол-во данных в буфере
                    if (dataList.Count == 0)
                        throw new Exception("Буфер обмена не содержит значений артикулов");

                    if (rgImportFld.Enabled)
                    {
                        if (rgImportFld.SelectedIndex == 0)
                            filterSet.Add(new InOperator(MasterDataGoodsFilter.Fields.lagerCodeVED, dataList));
                        else if (rgImportFld.SelectedIndex == 1)
                            filterSet.Add(new InOperator(MasterDataGoodsFilter.Fields.lagerClassifierSAPId, dataList));
                        else if (rgImportFld.SelectedIndex == 2)
                            filterSet.Add(new InOperator(MasterDataGoodsFilter.Fields.lagerId, dataList));
                        else
                            filterSet.Add(new InOperator(MasterDataGoodsFilter.Fields.barcode, dataList));
                    }
                    return filterSet;
                }
                throw new Exception("Буфер обмена не содержит значений артикулов");
            }
            catch (Exception ex)
            {
                MB.error(Ex.Message(ex));
                return filterSet;
            }

        }

        #endregion

        #region Обработчики TreeList

        private void SetCheckedChildNodes(TreeListNode node, CheckState checkState)
        {
            for (int i = 0; i < node.Nodes.Count; i++)
            {
                node.Nodes[i].CheckState = checkState;
                SetCheckedChildNodes(node.Nodes[i], checkState);
            }
        }

        private void SetCheckedParentNodes(TreeListNode node)
        {
            int countChecked = 0;

            for (int i = 0; i < node.Nodes.Count; i++)
            {
                if (node.Nodes[i].CheckState == CheckState.Indeterminate)
                    countChecked++;
                if (node.Nodes[i].CheckState == CheckState.Checked)
                    countChecked += 2;
            }

            if (countChecked == 2 * node.Nodes.Count)
                node.CheckState = CheckState.Checked;
            else if (countChecked == 0)
                node.CheckState = CheckState.Unchecked;
            else
                node.CheckState = CheckState.Indeterminate;
            
            if (node.ParentNode != null)
                SetCheckedParentNodes(node.ParentNode);
        }

        public static List<int> GetChildNodes(TreeListNode node)
        {
            var list = new List<int>();

            if (node.HasChildren)
                for (int i = 0; i < node.Nodes.Count; i++)
                    list.AddRange(GetChildNodes(node.Nodes[i]));
            else if (node.CheckState == CheckState.Checked)
                list.Add(int.Parse(node.GetValue(Fields.classifierId).ToString()));

            return list;
        }

        public static List<string> GetChildNodesNames(TreeListNode node)
        {
            var list = new List<string>();

            if (node.CheckState == CheckState.Checked)
                list.Add(node.GetValue(MasterDataGoodsFilter.Fields.classifierName).ToString());
            if (node.HasChildren)
                for (int i = 0; i < node.Nodes.Count; i++)
                    list.AddRange(GetChildNodesNames(node.Nodes[i]));
             if (node.CheckState == CheckState.Checked)
                list.Add(node.GetValue(MasterDataGoodsFilter.Fields.classifierName).ToString());

            return list;
        }

        public static List<string> GetTopChildNodes(TreeListNode node, string field)
        {
          List<string> list = new List<string>();

          if (node.CheckState == CheckState.Checked)
            list.Add(node.GetValue(field).ToString().Trim());

          if (node.HasChildren && node.CheckState != CheckState.Checked)
            for (int i = 0; i < node.Nodes.Count; i++)
              list.AddRange(GetTopChildNodes(node.Nodes[i], field));

          return list;
        }

        public void ClearChildNodes(TreeListNode node)
        {
            node.CheckState = CheckState.Unchecked;

            if (node.HasChildren)
            {for (int i = 0; i < node.Nodes.Count; i++)
                    ClearChildNodes(node.Nodes[i]);
            }
        }

        public void SetChildNodes(TreeListNode node, string[] data)
        {


            if (data.Contains(node.GetValue(Fields.classifierName).ToString()))
            {
                node.CheckState = CheckState.Checked;
                SetCheckedChildNodes(node, CheckState.Checked);
            }
            if (node.HasChildren)
            {

                for (int i = 0; i < node.Nodes.Count; i++)
                {
                    if (data.Contains(node.Nodes[i].GetValue(Fields.classifierName).ToString()))
                    {
                        node.Nodes[i].CheckState = CheckState.Checked;
                        SetCheckedParentNodes(node.Nodes[i]);
                    }
                        
                   SetChildNodes(node.Nodes[i], data);
                }
            }
           
        }

        public void SetChildNodesBySapId(TreeListNode node, string[] data)
        {
          if (data.Contains(node.GetValue(Fields.classifierSAPID).ToString().Trim()) && node.CheckState == CheckState.Unchecked)
          {
            node.CheckState = CheckState.Checked;
            if (node.HasChildren)
              SetCheckedChildNodes(node, CheckState.Checked);
            if (node.ParentNode != null)
              SetCheckedParentNodes(node.ParentNode);
          }
          else if (node.HasChildren)
            for (int i = 0; i < node.Nodes.Count; i++)
              SetChildNodesBySapId(node.Nodes[i], data);
        }

        #endregion

        #region Отбор товара по критериям

        /// <summary>
        /// Вызывается при завершении загрузки списка товаров
        /// </summary>
        protected event Action LoadGoodsFinishedEvent;

        /// <summary>
        /// Установка ChaseControl на контрол с гридом при отборе товара
        /// </summary>
        private void AddChaseControlOnSerch()
        {

            TGoodsFilterGrid.Items.ConvertToTypedList().ForEach(delegate(BaseLayoutItem item)
            {
                if (item is LayoutControlItem)
                {
                    if (((item as LayoutControlItem).Control is GridControl))
                    {
                        (item as LayoutControlItem).Control.Enabled = false;
                        ChaseControl.AddToControl((item as LayoutControlItem).Control, "Загрузка...");
                    }
                }
            });
            bOk.Enabled = false;
            lCFindGoods.Enabled = false;
            TGoodsFilter.PageEnabled = false;
        }

        /// <summary>
        /// Удаление ChaseControl с контрола с гридом при отборе товара
        /// </summary>
        private void RemoveChaseControlOnSerch()
        {
            TGoodsFilterGrid.Items.ConvertToTypedList().ForEach(delegate(BaseLayoutItem item)
            {
                if (item is LayoutControlItem)
                {
                    if (((item as LayoutControlItem).Control is GridControl))
                    {
                        (item as LayoutControlItem).Control.Enabled = true;
                        ChaseControl.RemoveFromControl((item as LayoutControlItem).Control);
                    }
                }
            });
            bOk.Enabled = true;
            lCFindGoods.Enabled = true;
            TGoodsFilter.PageEnabled = true;
        }

        /// <summary>
        /// Загрузка товаров из MasterData по фильтру или из буфера.
        /// </summary>
        private void GetGoods()
        {
            try
            {
                Filter request;

                if (rGTypeFilter.SelectedIndex == 0)
                {
                    // Применяем фильтр на основе критериев отбора
                    request = AssembleGoodsFilter();
                }
                else
                {
                    // Применяем значения вставленые из буфера
                    request = AssembleGoodsFilterFromClipboard();
                    if (ceUseFilter.Checked)
                        request.Add(AssembleGoodsFilter());
                }

                if (ReferenceEquals(request, null) || request.IsEmpty)
                    throw new Exception("Нет данных для отбора товара!");

                request.Add(new Filter(String.Format("[{0}] =  0", MasterDataGoodsFilter.Fields.lagerQuality)));

                var dimensions = new List<Dimension>();

                if (rgImportFld.Enabled && (rgImportFld.EditValue.ToString() == "3"))
                    dimensions.Add(new Dimension
                    {
                        columns =
                            new[]
                            {
                                Fields.lagerId, Fields.lagerName,
                                Fields.lagerUnit, Fields.lagerUnitTypeName,
                                Fields.lagerUnitQuantity, Fields.barcode
                            },
                        name = Dims.DimLagers,
                        operationNames = new[] {OperationName},
                        expression = request.Assemble(Filter.AssembleType.Sql),resultName = "dataGoods",
                        orderBy = new[] {new OrderBy {Value = MasterDataGoodsFilter.Fields.lagerName}}
                    });
                else
                    dimensions.Add(new Dimension
                    {
                        columns =
                            new[]
                            {
                                Fields.lagerId, Fields.lagerName,
                                Fields.lagerUnit, Fields.lagerUnitTypeName,
                                Fields.lagerUnitQuantity
                            },
                        name = Dims.DimLagers,
                        operationNames = new[] {OperationName},
                        expression = request.Assemble(Filter.AssembleType.Sql),
                        resultName = "dataGoods",
                        orderBy = new[] {new OrderBy {Value = Fields.lagerName}}
                    });


                goodsFilter = request;
                goodsRequest = request.Assemble(Filter.AssembleType.Sql);
                AddChaseControlOnSerch();
                FZCoreProxy.GetMasterDataAsyncStreamed(null, GetGoodsDataCallBack, new MasterDataRequest
                {
                    dim = dimensions.ToArray()
                });

            }
            catch (Exception ex)
            {
                MB.error(Ex.Message(ex));
            }
        }

        /// <summary>
        /// Загрузка товаров из MasterData по фильтру или из буфера. CallBack 
        /// </summary>
        private void GetGoodsDataCallBack(IDefaultContract o, object d)
        {
            var c = o as DefaultMessageContract;
            try
            {
                if (c == null || c.errorCode != ErrorCodes.OK)
                {
                    throw new Exception(o.ToString());
                }

                var reader = c.GetDataReader();
                if (reader.IsClosed)
                    throw new FZException(ErrorCodes.LOAD_DATA_ERROR, "reader is closed");

                using (DataTable table = c.CreateTable(reader, "dataGoods"))
                {
                    table.BeginLoadData();
                    Application.DoEvents();
                    while (true)
                    {
                        if (!reader.Read())
                        {
                            table.EndLoadData();
                            if (!reader.NextResult())
                                break;
                            // Ожидаем только одну таблицу, поэтому выходим.
                            break;
                        }
                        var values = new object[reader.FieldCount];
                        reader.GetValues(values);

                        DataRow row = table.NewRow();
                        row.ItemArray = values;
                        table.Rows.Add(row);
                    }
                    if (ThGoods.SelectedTabPage != null && ThGoods.SelectedTabPage != TGoodsFilterGrid)
                        ThGoods.SelectedTabPage = TGoodsFilterGrid;

                    if (!table.Columns.Contains("AplyGoods"))
                    {
                        table.Columns.Add("AplyGoods", typeof (bool));
                    }
                    // Присваиваем значение по умолчанию для поля штучного выбора артикулов
                    foreach (DataRow row in table.Rows)
                        row["AplyGoods"] = "true";

                    if ((cEClearData.Checked) || (goodsGrid.DataSource == null))
                        goodsGrid.DataSource = table;
                    else
                    {
                        var tmpStore = (goodsGrid.DataSource as DataTable);
                        if (tmpStore != null)
                        {
                            tmpStore.PrimaryKey = new[] {tmpStore.Columns[0]};
                            //TmpStore.Load(table.CreateDataReader(), LoadOption.Upsert);
                            tmpStore.Merge(table);
                            goodsGrid.DataSource = tmpStore;
                        }
                    }

                    if (LoadGoodsFinishedEvent != null)
                        LoadGoodsFinishedEvent();

                }
            }
            catch (Exception ex)
            {
                MB.error(ex);
                RemoveChaseControlOnSerch();
            }
        }

        #endregion

        #region Очистка формы и данных

        private void ClearFilterAndData()
        {
            layoutControl.Items.ConvertToTypedList().ForEach(delegate(BaseLayoutItem item)
            {
                if (item is LayoutControlItem)
                {
                    Control o = (item as LayoutControlItem).Control;

                    if (o is CheckedComboBoxEdit)
                    {
                        (o as CheckedComboBoxEdit).SetEditValue(String.Empty);
                        (o as CheckedComboBoxEdit).DeselectAll();
                    }
                    else if (o is LookUpEdit)
                    {
                        (o as LookUpEdit).EditValue = null;
                    }
                    else if (o is RadioGroup)
                    {
                        (o as RadioGroup).SelectedIndex = 0;
                    }
                    else if (o is TreeList)
                    {
                        (o as TreeList).BeginUpdate();
                        try
                        {
                            for (int i = 0; i < (o as TreeList).Nodes.Count; i++)
                            {
                                ClearChildNodes((o as TreeList).Nodes[i]);
                            }
                        }
                        finally
                        {
                            (o as TreeList).EndUpdate();
                            (o as TreeList).CollapseAll();
                        }
                    }
                    else if (o is TextEdit)
                    {
                        (o as TextEdit).Text = String.Empty;
                    }
                }
            });

            goodsGrid.DataSource = null;
            cEClearData.Checked = true;
            rGTypeFilter.SelectedIndex = 0;
            goodsRequest = String.Empty;
            IsChecked = true;
            IsClearData = true;
            IsFromBuffer = false;
        }

        #endregion

        #region Обработка нажатия княпок

        private void bOk_Click(object sender, EventArgs e)
        {

            SaveCacheParameters();

            Parameters = null;
            Parameters = SignFilter.GetFilter();

            var executedFilter = new Filter(GroupOperatorType.And);
            _filterHint.Clear();

            try
            {
                var dataTable = goodsGrid.DataSource as DataTable;
                if (dataTable != null)
                    IsChecked = goodsGrid.DataSource == null || !dataTable.Select("AplyGoods = False").Any();

                var dataList = new List<int>();
                if ((IsClearData == false) || (IsFromBuffer) || (!IsChecked))
                {
                    var table = goodsGrid.DataSource as DataTable;
                    if (table != null && (goodsGrid.DataSource == null || (!table.Select("AplyGoods = True").Any())))
                        throw new Exception("Нет выбраных товаров для отбора!");

                    var dataTableGoods = goodsGrid.DataSource as DataTable;
                    if (dataTableGoods != null)
                        dataList.AddRange(from DataRow row in dataTableGoods.Rows
                            where row["AplyGoods"].ToString() == "True"
                            select int.Parse(row["Lagers.lagerId"].ToString()));


                    CriteriaOperator co = new InOperator(goodsGrid.Tag.ToString(), dataList);
                    if (cEExcludeGoods.Checked) co = new NotOperator(co);
                    executedFilter.Add(co);
                    executedFilter.Add(
                        new Filter(String.Format("[{0}] =  0", MasterDataGoodsFilter.Fields.lagerQuality)));
                    goodsFilter = executedFilter;
                    goodsRequest = executedFilter.Assemble(Filter.AssembleType.Sql);
                    GoodsArray = dataList.ToArray();

                    List<string> sl = dataList.ConvertAll(i => i.ToString(CultureInfo.InvariantCulture));

                    if (sl.Count < 10)
                    {
                        _filterHint.Add(
                            String.Format(
                                cEExcludeGoods.Checked ? HintTemplates["lagerSetExclude"] : HintTemplates["lagerSet"],
                                String.Join(", ", sl.ToArray())));
                    }
                    else
                    {
                        _filterHint.Add(
                            String.Format(
                                cEExcludeGoods.Checked
                                    ? HintTemplates["lagerSetLargeExclude"]
                                    : HintTemplates["lagerSetLarge"], sl.Count));
                    }


                    DialogResult = DialogResult.OK;
                    //dataList.ConvertAll<string>(delegate(int i){ return i.toString(); })
                }
                else
                {
                    executedFilter = AssembleGoodsFilter();

                        executedFilter.Add(
                            new Filter(String.Format("[{0}] =  0", MasterDataGoodsFilter.Fields.lagerQuality)));
                        goodsRequest = executedFilter.Assemble(Filter.AssembleType.Sql);
                    


                    CacheControl();


                    DialogResult = DialogResult.OK;
                }
            }
            catch (Exception ex)
            {
                MB.error(Ex.Message(ex));
            }
        }

        private void SaveCacheParameters()
        {

            var saveParameters = new SaveParameters
            {
                Sign = checkEditSign.Checked,
                SignValue = checkEditSignValues.Checked,
                AllParameters = checkEditAllParameters.Checked
            };

            CacheHelper.GetHelper("FilterProperties").Put(SaveSettingsKey, Serialization.Serialize(saveParameters));
        }
        private void LoadCacheParameters()
        {

            CacheParameters = null;
            string settingsString = CacheHelper.GetHelper("FilterProperties").Get(SaveSettingsKey);

            {
                try
                {
                    CacheParameters = (String.IsNullOrEmpty(settingsString)) ?
                       new SaveParameters()
                       :
                       Serialization.Deserialize<SaveParameters>(settingsString);


                    checkEditSign.Checked = CacheParameters.Sign;
                    checkEditSignValues.Checked = CacheParameters.SignValue;
                    checkEditAllParameters.Checked = CacheParameters.AllParameters;


                }
                catch (Exception ex)
                {
                    MB.error(ex);
                }
            }
        }


        private void BClearData_Click(object sender, EventArgs e)
        {
            CacheAllControls.Controls = null;
            CacheAllControls.Parameters = null;
            ClearFilterAndData();
            GoodsArray = null;
            SignFilter.SetDefaultFilter();
        }

        private void BGetData_Click(object sender, EventArgs e)
        {
            Parameters = null;
            Parameters = SignFilter.GetFilter();
            GetGoods();
            IsClearData = cEClearData.Checked;
            if (rGTypeFilter != null) IsFromBuffer = rGTypeFilter.SelectedIndex != 0;
        }

        private void BCheckedAll_Click(object sender, EventArgs e)
        {
            // Проверяем не пуст ли грид отбора артикулов, есть ли в нем выбраные артикула
            var table = goodsGrid.DataSource as DataTable;
            if (table != null && (goodsGrid.DataSource != null && table.Rows.Count > 0))
            {
                // Выбраных артикулов нет
                // Выбираем в гриде отбора артикулов все артикула
                var dataTable = goodsGrid.DataSource as DataTable;
                foreach (DataRow row in dataTable.Rows)
                    row["AplyGoods"] = "true";
            }
        }

        private void BUnCheckedAll_Click(object sender, EventArgs e)
        {
            // Проверяем не пуст ли грид отбора артикулов, есть ли в нем выбраные артикула
            var dataTable = goodsGrid.DataSource as DataTable;
            if (dataTable != null && (goodsGrid.DataSource != null && dataTable.Rows.Count > 0))
            {
                // Выбраных артикулов нет
                // Отменяем в гриде отбора артикулов все артикула
                foreach (DataRow row in (goodsGrid.DataSource as DataTable).Rows)
                    row["AplyGoods"] = "false";
            }
        }

        private void rGTypeFilter_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        #endregion

        private void ELagerId_ButtonClick_1(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button.Kind == ButtonPredefines.Ellipsis)
            {
                using (var form = new LagersList())
                {
                    form.lagerIds = GoodsArray;
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        GoodsArray = form.lagerIds;
                        if (GoodsArray.Count() > 1)
                        {
                            ELagerId.Properties.Mask.MaskType = MaskType.None;
                            ELagerId.Text = @"набор";
                            ELagerId.Properties.ReadOnly = true;
                            foreach (EditorButton eb in ELagerId.Properties.Buttons)
                                if (eb.Kind == ButtonPredefines.Delete) eb.Visible = true;
                        }
                    }
                }
            }
            else if (e.Button.Kind == ButtonPredefines.Delete)
            {
                GoodsArray = new int[0];
                ELagerId.Text = "";
                ELagerId.Properties.ReadOnly = false;
                e.Button.Visible = false;
            }
        }

        private void rgImportFld_Properties_EditValueChanging(object sender, ChangingEventArgs e)
        {
            gCBarcode.VisibleIndex = e.NewValue.ToString() == "3" ? 0 : -1;
        }

        private void tLSapClassifier_AfterCheckNode(object sender, NodeEventArgs e)
        {
            if (e.Node.CheckState == CheckState.Indeterminate && sender != null)
                e.Node.CheckState = CheckState.Checked;

            tLSapClassifier.BeginUpdate();
            try
            {
                SetCheckedChildNodes(e.Node, e.Node.CheckState);

                if (e.Node.ParentNode != null)  //  && e.Node.CheckState == 0          
                    SetCheckedParentNodes(e.Node.ParentNode);
            }
            finally
            {
                tLSapClassifier.EndUpdate();
            }
        }

        private void tLSapClassifier_NodeChanged_1(object sender, NodeChangedEventArgs e)
        {
            if (e.ChangeType == NodeChangeTypeEnum.Add)
                tLSapClassifier.FocusedNode = tLSapClassifier.Nodes[0];
        }

        /// <summary>
        /// В гриде отбора артикулов ставим/убераем выбор артикула по двойному клику
        /// </summary>
        private void goodsGrid_DoubleClick(object sender, EventArgs e)
        {
            // если грид пустой, то ничего не делаем
            if (goodsGridView.DataRowCount == 0)
                return;

            goodsGridView.SetFocusedRowCellValue("AplyGoods",
                goodsGridView.GetRowCellValue(goodsGridView.FocusedRowHandle, gCChecked).ToString() == "True"
                    ? "False"
                    : "True");
        }

        private void bExpand_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            tLSapClassifier.ExpandAll();
        }

        private void bCallapse_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            tLSapClassifier.CollapseAll();
        }
    }
}
