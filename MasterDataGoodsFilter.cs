using System;
using System.Collections.Generic;
using System.Data;
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
using FozzySystems.Proxy;
using FozzySystems.Types;
using FozzySystems.Types.Contracts;
using FozzySystems.Types.Filtering;
using FozzySystems.Utils;

namespace FZComponents.Dialogs
{
  public partial class MasterDataGoodsFilter : XtraForm
  {
    public static DataSet MasterDataSet;
    private List<string> _filterHint;
    public string FilterHint
    {
      get
      {
        return ((_filterHint == null) || (_filterHint.Count == 0)) ? "Все товары" : String.Join("; ", _filterHint.ToArray());
      }
    }

    public MasterDataGoodsFilter(string operationName, string goodsRequest = null)
    {
      InitializeComponent();

      _filterHint = new List<string>();

      FormClosing += (s, e) =>
      {
        // если форму закрыли в процессе загрузки мастерданных,
        // то отменим запрос
        if (MasterDataRequestGuid != Guid.Empty)
        {
          FZCoreProxy.CancelConnection(MasterDataRequestGuid);
        }
      };


      if (!String.IsNullOrEmpty(goodsRequest))
      {
        this.goodsRequest = goodsRequest;
        LoadDictionaryFinishedEvent += DisassembleGoodsFilter;
      }
      else
      {
        LoadDictionaryFinishedEvent += RemoveChaseControlOnFilter;
      }
      LoadGoodsFinishedEvent += RemoveChaseControlOnSerch;

      this.operationName = operationName;

      LoadControlParams();

      AddChaseControlOnFilter();
      GetGoodsDataDictionary();

      isChecked = true;
      isClearData = true;
      isFromBuffer = false;
    }

    #region Глобальные переменные и загрузки значений контролов
    private Guid MasterDataRequestGuid = Guid.Empty;
    
    public Boolean isClearData;
    public Boolean isFromBuffer;
    public Boolean isChecked;
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
    public int[] goodsArray { get; private set; }
    /// <summary>
    /// Операция в корой используется фильтр для определения разрешений пользователя
    /// </summary>
    public string operationName
    {
      get;
      set;
    }
    /// <summary>
    /// Класс содержащий название полей данных справочников
    /// </summary>
    public struct Fields
    {
      public const string lagerId = "Lagers.lagerId";
      public const string barcode = "Barcodes.barcode";
      public const string lagerName = "Lagers.lagerName";
      public const string lagerUnit = "Lagers.lagerUnit";
      public const string lagerQuality = "Lagers.lagerQuality";
      public const string lagerClassifierId = "Lagers.lagerClassifier";
      public const string lagerClassifierSAPId = "Lagers.lagerClassifierSAPId";
      public const string lagerCodeVED = "Lagers.lagerCodeVED";
      public const string lagerUnitTypeName = "ListLagerUnitTypes.lagerUnitTypeName";
      public const string lagerUnitQuantity = "Lagers.lagerUnitQuantity";


      public const string departmentId = "Departments.departmentIdSap";
      public const string departmentName = "Departments.departmentName";

      public const string macroId = "MacroGroups.macroId";
      public const string macroName = "MacroGroups.macroName";

      public const string lagerTypeId = "LagerTypes.lagerTypeId";
      public const string lagerTypeName = "LagerTypes.lagerTypeName";

      public const string gentlemanSetId = "GentlemanSets.gentlemanSetId";
      public const string gentlemanSetName = "GentlemanSets.gentlemanSetName";

      public const string brandId = "Brands.brandId";
      public const string brandName = "Brands.brandName";

      public const string purchaseGroupSapId = "PurchaseGroups.purchaseGroupSapId";
      public const string purchaseGroupName = "PurchaseGroups.purchaseGroupName";

      public const string classifierId = "classifierId";
      public const string classifierParentId = "classifierParentId";
      public const string classifierSAPID = "classifierSapId";
      public const string classifierName = "classifierName";

      public const string lagerPrivateLabel = "Lagers.lagerPrivateLabel";
      public const string lagerOwnImport = "Lagers.lagerOwnImport";
      public const string lagerIndicativeSoc = "Lagers.lagerLimitedMarginPercent";
      public const string lagerIndicativeAlc = "Lagers.isAlcoholIndicative";

      public const string VAT = "Lagers.lagerNds";
    }


    public Dictionary<string, string> HintTemplates = new Dictionary<string, string>()
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
    /// Класс содержащий название звезд справочников
    /// </summary>
    public struct Dims
    {
      public const string dim_Lagers = "dim_Lagers";
      public const string dim_LagerSapClassifier = "dim_LagerSapClassifier";
    }

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
      cBGentlemanSet.Properties.ValueMember = Fields.gentlemanSetId;
      cBGentlemanSet.Properties.DisplayMember = Fields.gentlemanSetName;
      cBGentlemanSet.Tag = Fields.gentlemanSetId;
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
          List<Dimension> dimensions = new List<Dimension>();

          dimensions.Add(new Dimension()
          {
            columns = new string[] { Fields.macroId, Fields.macroName },
            name = Dims.dim_Lagers,
            operationNames = new string[] { operationName },
            resultName = "MacroGroups",
            orderBy = new OrderBy[] { new OrderBy() { Value = Fields.macroName } }
          });
          dimensions.Add(new Dimension()
          {
            columns = new string[] { Fields.departmentId, Fields.departmentName },
            name = Dims.dim_Lagers,
            operationNames = new string[] { operationName },
            resultName = "Departments",
            orderBy = new OrderBy[] { new OrderBy() { Value = Fields.departmentName } }
          });
          dimensions.Add(new Dimension()
          {
            columns = new string[] { Fields.lagerTypeId, Fields.lagerTypeName },
            name = Dims.dim_Lagers,
            operationNames = new string[] { operationName },
            resultName = "LagerTypes",
            orderBy = new OrderBy[] { new OrderBy() { Value = Fields.lagerTypeName } }
          });
          dimensions.Add(new Dimension()
          {
            columns = new string[] { Fields.brandId, Fields.brandName },
            name = Dims.dim_Lagers,
            operationNames = new string[] { operationName },
            resultName = "Brands",
            orderBy = new OrderBy[] { new OrderBy() { Value = Fields.brandName } }
          });
          dimensions.Add(new Dimension()
          {
            columns = new string[] { Fields.gentlemanSetId, Fields.gentlemanSetName },
            name = Dims.dim_Lagers,
            operationNames = new string[] { operationName },
            resultName = "GentlemanSets",
            orderBy = new OrderBy[] { new OrderBy() { Value = Fields.gentlemanSetName } }
          });
          dimensions.Add(new Dimension()
          {
            columns = new string[] { Fields.purchaseGroupSapId, Fields.purchaseGroupName },
            name = Dims.dim_Lagers,
            operationNames = new string[] { operationName },
            resultName = "PurchaseGroups",
            expression = String.Format("ISNULL({0}, '') != ''", Fields.purchaseGroupSapId),
            orderBy = new OrderBy[] { new OrderBy() { Value = Fields.purchaseGroupName } }
          });
          dimensions.Add(new Dimension()
          {
            columns = new string[] { Fields.classifierId, Fields.classifierParentId, Fields.classifierSAPID, Fields.classifierName },
            name = Dims.dim_LagerSapClassifier,
            operationNames = new string[] { operationName },
            resultName = "LagerSapClassifier"
          });


          MasterDataRequestGuid = FZCoreProxy.GetMasterDataAsync(null, GetGoodsDataDictionaryCallBack, new MasterDataRequest()
          {
            dim = dimensions.ToArray()
          });
        }
        catch (Exception ex)
        {
          MB.error(ex);
          this.DialogResult = DialogResult.Cancel;
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
        if ((this.IsDisposed) && (o.errorCode == ErrorCodes.CANCELLED))
          return;

        if (o != null && o.errorCode != ErrorCodes.OK)
          throw new Exception(o.errorString);

        MasterDataSet = (o as MasterDataContract).dataSet;
        DataColumn col = new DataColumn();
        col.DataType = typeof(string);
        col.Expression = Fields.classifierSAPID + " + ' ' + " + Fields.classifierName;
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
        if (!this.IsDisposed)
          MasterDataRequestGuid = Guid.Empty;
      }
    }


    private void SetDataToControls()
    {
      if (this.IsDisposed)
      {
        return;
      }

      //Установка DataSet в контролы фильтрации
      cBDepartment.Properties.DataSource = MasterDataSet.Tables["Departments"];
      cBMacrogroup.Properties.DataSource = MasterDataSet.Tables["MacroGroups"];
      cBGentlemanSet.Properties.DataSource = MasterDataSet.Tables["GentlemanSets"];
      cBPurchaseGroup.Properties.DataSource = MasterDataSet.Tables["PurchaseGroups"];
      lEBrand.Properties.DataSource = MasterDataSet.Tables["Brands"];
      cBLagerType.Properties.DataSource = MasterDataSet.Tables["LagerTypes"];
      tLSapClassifier.DataSource = MasterDataSet.Tables["LagerSapClassifier"];
      tLSapClassifier.Columns.ColumnByFieldName(Fields.classifierName).Visible = false;
      tLSapClassifier.Columns.ColumnByFieldName(Fields.classifierSAPID).Visible = false;
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
      List<int> li = new List<int>();
      //String requestPart;
      Filter filterSet = new Filter(GroupOperatorType.And);
      _filterHint.Clear();

      foreach (BaseLayoutItem item in layoutControl.Items)
      {
        if (!(item is LayoutControlItem))
          continue;


        object o = (item as LayoutControlItem).Control;
        if ((o == null) || (!(o is Control)) || (((o as Control).Tag == null)))
          continue;

        string fieldTag = (o as Control).Tag.ToString();
        string hintText = "";

        if (o is CheckedComboBoxEdit)
        {
          if (!String.IsNullOrEmpty((o as CheckedComboBoxEdit).Properties.GetCheckedItems().ToString()))
          {
            CheckedComboBoxEdit cB = (o as CheckedComboBoxEdit);
            if ((cB.Properties.DataSource as DataTable).Columns[fieldTag].DataType.ToString().Contains("Int"))
            {
              filterSet.Add(new Filter(String.Format("[{0}] in ({1})", fieldTag, cB.Properties.GetCheckedItems().ToString())));
            }
            else
            {
              filterSet.Add(new InOperator(fieldTag, ((string)cB.Properties.GetCheckedItems()).Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries)));
            }
            hintText = cB.Properties.GetDisplayText(null);
          }
        }
        else if (o is LookUpEdit)
        {
          if ((o as LookUpEdit).EditValue != null)
          {
            LookUpEdit lE = (o as LookUpEdit);
            filterSet.Add(new Filter(String.Format("[{0}] in ({1})", fieldTag, lE.GetColumnValue(lE.Properties.Columns[fieldTag]).ToString())));
            hintText = lE.Properties.GetDisplayText(lE.EditValue).Trim();
          }
        }
        else if (o is RadioGroup)
        {
          RadioGroup rG = o as RadioGroup;
          if (rG.Equals(rgVAT))
          {
            if (rgVAT.SelectedIndex > 0)
              filterSet.Add(new Filter(String.Format("isnull([{0}], 0) {1}", fieldTag, rG.EditValue.ToString())));
          }
          else
            filterSet.Add(new Filter(rG.Properties.Items[rG.SelectedIndex].Value != null ? rG.Properties.Items[rG.SelectedIndex].Value.ToString() : null));
        }
        else if (o is TreeList)
        {
          TreeList tL = (o as TreeList);
          List<int> dataList = new List<int>();
          List<string> dataListNames = new List<string>();
          for (int i = 0; i < tL.Nodes.Count; i++)
          {
            dataList.AddRange(GetChildNodes(tL.Nodes[i]));
            dataListNames.AddRange(GetChildNodesNames(tL.Nodes[i]));
          }
          if (dataList.Count != 0)
          {
            InOperator io = new InOperator(fieldTag, dataList);
            hintText = String.Join(", ", dataListNames.ToArray());
            if (!cEExcludeSapClassifier.Checked)
            {
              filterSet.Add(io);
            }
            else
            {
              filterSet.Add(new NotOperator(io));
              hintText = "все, кроме " + hintText;
            }
          }
        }
        else if (o is ComboBoxEdit)
        {
          ComboBoxEdit cB = (o as ComboBoxEdit);

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
        else if (o is TextEdit)
        {
          if (!String.IsNullOrEmpty((o as TextEdit).Text))
          {
            TextEdit tE = (o as TextEdit);
            if (fieldTag == Fields.lagerId)
            {
              try
              {
                if (tE.Text == "набор")
                {
                  li.AddRange(goodsArray);
                  hintText = "(" + String.Join(", ", li.Select(i => i.ToString()).ToArray()) + ")";
                }
                else
                {
                  li.Add(int.Parse(tE.Text));
                  hintText = tE.Text;
                }

                InOperator io = new InOperator(fieldTag, li);
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
              catch (Exception)
              {
              }
            }
            else if (fieldTag == Fields.lagerUnit)
            {
              if (cBEdIzmCompareType.SelectedIndex == 3) // не равно
              {
                filterSet.Add(new Filter(String.Format("[{0}] <> '{1}'", fieldTag, Sql.SafeSqlLiteral(tE.Text))));
                hintText = "не " + tE.Text;
              }
              else if (cBEdIzmCompareType.SelectedIndex == 2) // равно
              {
                filterSet.Add(new Filter(String.Format("[{0}] = '{1}'", fieldTag, Sql.SafeSqlLiteral(tE.Text))));
                hintText = tE.Text;
              }
              else if (cBEdIzmCompareType.SelectedIndex == 1) // не содержит
              {
                filterSet.Add(new Filter(String.Format("[{0}] not like '%{1}%'", fieldTag, Sql.SafeSqlLiteral(tE.Text))));
                hintText = String.Format("не содержит '{0}'", tE.Text);
              }
              else // содержит
              {
                filterSet.Add(new Filter(String.Format("[{0}] like '%{1}%'", fieldTag, Sql.SafeSqlLiteral(tE.Text))));
                hintText = String.Format("содержит '{0}'", tE.Text);
              }
            }
            else
            {
              filterSet.Add(new Filter(String.Format("[{0}] like '%{1}%'", fieldTag, Sql.SafeSqlLiteral(tE.Text))));
              hintText = tE.Text;
            }
          }
        }
        else if (o is CheckEdit)
        {
          if ((o as CheckEdit).Checked)
          {
            filterSet.Add(new Filter(String.Format("[{0}] = 1", fieldTag)));
          }
        }

        // добавляем хинт по полю если есть шаблон для этого поля и текст хинта не пустой          
        if ((!String.IsNullOrEmpty(hintText)) && (HintTemplates.ContainsKey(fieldTag)))
          _filterHint.Add(String.Format(HintTemplates[fieldTag], hintText));
      }

      /*
       layoutControl.Items.ConvertToTypedList().ForEach(delegate(BaseLayoutItem item)
            {

              if (item is LayoutControlItem)
              {
                object o = (item as LayoutControlItem).Control;
                string fieldTag = ((!(o is Control)) || ((o as Control).Tag == null)) ? null : (o as Control).Tag.ToString();
                string hintText = "";

                if (!String.IsNullOrEmpty(fieldTag))
                {
                  if (o is CheckedComboBoxEdit)
                  {
                    if (!String.IsNullOrEmpty((o as CheckedComboBoxEdit).Properties.GetCheckedItems().ToString()))
                    {
                      CheckedComboBoxEdit cB = (o as CheckedComboBoxEdit);
                      if ((cB.Properties.DataSource as DataTable).Columns[fieldTag].DataType.ToString().Contains("Int"))
                      {
                        filterSet.Add(new Filter(String.Format("[{0}] in ({1})", fieldTag, cB.Properties.GetCheckedItems().ToString())));
                        hintText = cB.Properties.GetDisplayText(null);
                      }
                      else
                      {
                        filterSet.Add(new InOperator(fieldTag, ((string)cB.Properties.GetCheckedItems()).Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries)));
                      }

                    }
                  }
                  else if (o is LookUpEdit)
                  {
                    if ((o as LookUpEdit).EditValue != null)
                    {
                      LookUpEdit lE = (o as LookUpEdit);
                      filterSet.Add(new Filter(String.Format("[{0}] in ({1})", fieldTag, lE.GetColumnValue(lE.Properties.Columns[fieldTag]).ToString())));
                      hintText = lE.GetColumnValue(lE.Properties.Columns[fieldTag]).ToString();
                    }
                  }
                  else if (o is RadioGroup)
                  {
                    RadioGroup rG = o as RadioGroup;
                    filterSet.Add(new Filter(rG.Properties.Items[rG.SelectedIndex].Value != null ? rG.Properties.Items[rG.SelectedIndex].Value.ToString() : null));
                  }
                  else if (o is TreeList)
                  {
                    TreeList tL = (o as TreeList);
                    List<int> dataList = new List<int>();
                    for (int i = 0; i < tL.Nodes.Count; i++)
                    {
                      dataList.AddRange(GetChildNodes(tL.Nodes[i]));
                    }
                    if (dataList.Count != 0)
                      filterSet.Add(new InOperator(fieldTag, dataList));
                  }
                  else if (o is ComboBoxEdit)
                  {
                    ComboBoxEdit cB = (o as ComboBoxEdit);
                    switch (cB.SelectedIndex)
                    {
                      case 1:
                        filterSet.Add(new Filter(String.Format("[{0}] = 1", fieldTag)));
                        break;
                      case 2:
                        filterSet.Add(new Filter(String.Format("[{0}] = 0", fieldTag)));
                        break;

                    }
                  }
                  else if (o is TextEdit)
                  {
                    if (!String.IsNullOrEmpty((o as TextEdit).Text))
                    {
                      TextEdit tE = (o as TextEdit);
                      if (fieldTag == Fields.lagerId)
                      {
                        filterSet.Add(new InOperator(fieldTag, int.Parse(tE.Text)));
                        hintText = tE.Text;
                      }
                      else
                      {
                        filterSet.Add(new Filter(String.Format("[{0}] like '%{1}%'", fieldTag, Sql.SafeSqlLiteral(tE.Text))));
                        hintText = tE.Text;
                      }
                    }
                  }
           
                  // добавляем хинт по полю если есть шаблон для этого поля и текст хинта не пустой          
                  if ((!String.IsNullOrEmpty(hintText)) && (HintTemplates.ContainsKey(fieldTag)) && (!String.IsNullOrEmpty(hintText)))
                    _filterHint.Add(String.Format(HintTemplates[fieldTag], hintText));
                }
              }
            });
       */
      goodsArray = li.ToArray();
      goodsFilter = filterSet;
      return filterSet;
    }

    /// <summary>
    /// Разбор FilterSet по переданой строке
    /// </summary>
    public Filter AssembleGoodsFilterFromClipboard()
    { // Процедура создает фильтр на основе данных из буфера обмена
      Filter filterSet = new Filter(GroupOperatorType.And);
      try
      {
        bool isLagers = (rgImportFld.Enabled) && (rgImportFld.SelectedIndex == 2);
        // Забираем список артикулов из буфера обмена
        IDataObject iData = new DataObject();
        iData = Clipboard.GetDataObject();

        if (iData.GetDataPresent(DataFormats.Text))
        {
          string s = (string)iData.GetData(typeof(String));
          List<object> dataList = new List<object>();

          int d;

          string[] items = s.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);


          for (int i = 0; i < (items.Length <= 5000 ? items.Length : 5000); i++)
          {
            if (isLagers)
            {
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
              filterSet.Add(new InOperator(Fields.lagerCodeVED, dataList));
            else
              if (rgImportFld.SelectedIndex == 1)
                filterSet.Add(new InOperator(Fields.lagerClassifierSAPId, dataList));
              else
                if (rgImportFld.SelectedIndex == 2)
                  filterSet.Add(new InOperator(Fields.lagerId, dataList));
                else
                  filterSet.Add(new InOperator(Fields.barcode, dataList));
          }
          return filterSet;
        }
        else
          throw new Exception("Буфер обмена не содержит значений артикулов");
      }
      catch (Exception ex)
      {
        MB.error(Ex.Message(ex));
        return filterSet;
      }

    }

    /// <summary>
    /// Разбор FilterSet по переданой строке
    /// </summary>
    private void DisassembleGoodsFilter()
    {
      try
      {
        List<string> dataList = new List<string>(goodsRequest.Split(new string[] { " And " }, StringSplitOptions.RemoveEmptyEntries));
        int rowIndex;
        rowIndex = dataList.FindIndex(p => p.Contains(Fields.lagerId));
        if (rowIndex >= 0 ? !getDataFromRequest(dataList[rowIndex]).Contains(",") : true)
        {
          layoutControl.Items.ConvertToTypedList().ForEach(delegate(BaseLayoutItem item)
          {
            if (item is LayoutControlItem)
            {
              object o = (item as LayoutControlItem).Control;

              if (o != cBEdIzmCompareType)
                if (o is CheckedComboBoxEdit)
                {
                  CheckedComboBoxEdit cB = (o as CheckedComboBoxEdit);
                  rowIndex = dataList.FindIndex(p => p.Contains(cB.Tag.ToString()));
                  if (rowIndex >= 0)
                    cB.SetEditValue(getDataFromRequest(dataList[rowIndex]));
                }
                else if (o is LookUpEdit)
                {
                  LookUpEdit lE = (o as LookUpEdit);
                  rowIndex = dataList.FindIndex(p => p.Contains(lE.Tag.ToString()));
                  if (rowIndex >= 0)
                    lE.EditValue = Convert.ToInt32(getDataFromRequest(dataList[rowIndex]));
                }
                else if (o is RadioGroup)
                {
                  RadioGroup rG = (o as RadioGroup);
                  rowIndex = dataList.FindIndex(p => p.Contains(rG.Tag.ToString()));
                  if (rowIndex >= 0)
                  {
                    if (rgVAT.Equals(rgVAT))
                    {
                      if (dataList[rowIndex].Contains("="))
                        rgVAT.SelectedIndex = 2;
                      else
                        rgVAT.SelectedIndex = 1;
                    }
                    else
                      switch (int.Parse(getDataFromRequest(dataList[rowIndex])))
                      {
                        case 0:
                          rG.SelectedIndex = 1;
                          break;
                        case 1:
                          rG.SelectedIndex = 2;
                          break;
                      }
                  }
                  else
                    rG.SelectedIndex = 0;
                }
                else if (o is TreeList)
                {
                  TreeList tL = (o as TreeList);
                  rowIndex = dataList.FindIndex(p => p.Contains(tL.Tag.ToString()));
                  if (rowIndex >= 0)
                  {
                    tL.BeginUpdate();
                    try
                    {
                      for (int i = 0; i < tL.Nodes.Count; i++)
                      {
                        SetChildNodes(tL.Nodes[i], getDataFromRequest(dataList[rowIndex]).Replace(" ", "").Split(','));
                      }
                    }
                    finally
                    {
                      tL.EndUpdate();
                      tL.CollapseAll();
                    }
                    if (dataList[rowIndex].Contains("Not"))
                      cEExcludeSapClassifier.Checked = true;
                  }
                }
                else if (o is ComboBoxEdit)
                {
                  ComboBoxEdit cB = (o as ComboBoxEdit);
                  rowIndex = dataList.FindIndex(p => p.Contains(cB.Tag.ToString()));
                  if (rowIndex >= 0)
                  {
                    if (int.Parse(getDataFromRequest(dataList[rowIndex])) == 1)
                      cB.SelectedIndex = 1;
                    else
                      cB.SelectedIndex = 2;
                  }
                  else
                    cB.SelectedIndex = 0;
                }
                else if (o is TextEdit)
                {
                  TextEdit tE = (o as TextEdit);
                  rowIndex = dataList.FindIndex(p => p.Contains(tE.Tag.ToString()));
                  if (rowIndex >= 0)
                  {
                    tE.Text = getDataFromRequest(dataList[rowIndex]);
                    if (tE.Tag.ToString() == Fields.lagerId && dataList[rowIndex].Contains("Not"))
                      cEExcludeLagerId.Checked = true;
                    else if (tE.Tag.ToString() == Fields.lagerUnit)
                    {
                      if (dataList[rowIndex].Contains("Like"))
                      {
                        if (!dataList[rowIndex].Contains("Not"))
                          cBEdIzmCompareType.SelectedIndex = 0; // содержит
                        else
                          cBEdIzmCompareType.SelectedIndex = 1; // не содержит
                      }
                      else
                      {
                        if (!dataList[rowIndex].Contains("<>"))
                          cBEdIzmCompareType.SelectedIndex = 2; // равно
                        else
                          cBEdIzmCompareType.SelectedIndex = 3; // не равно
                      }
                    }
                  }
                }
            }
          });
        }
        else
        {
          Filter request = new Filter(GroupOperatorType.And);
          List<Dimension> dimensions = new List<Dimension>();
          isFromBuffer = true;
          request.Add(new Filter(dataList[rowIndex]));
          request.Add(new Filter(String.Format("[{0}] =  0", Fields.lagerQuality)));

          dimensions.Add(new Dimension()
          {
            columns = new string[] { Fields.lagerId, Fields.lagerName, Fields.lagerUnit, Fields.lagerUnitTypeName, Fields.lagerUnitQuantity },
            name = Dims.dim_Lagers,
            operationNames = new string[] { operationName },
            expression = request.Assemble(Filter.AssembleType.Sql),
            resultName = "dataGoods",
            orderBy = new OrderBy[] { new OrderBy() { Value = Fields.lagerName } }
          });

          goodsRequest = request.Assemble(Filter.AssembleType.Sql);
          AddChaseControlOnSerch();
          FZCoreProxy.GetMasterDataAsyncStreamed(null, GetGoodsDataCallBack, new MasterDataRequest()
          {
            dim = dimensions.ToArray()
          });

        }
      }
      catch (Exception ex)
      {
        MB.error(Ex.Message(ex));
      }
      finally
      {
        goodsRequest = string.Empty;
        RemoveChaseControlOnFilter();
      }
    }

    private string getDataFromRequest(string data)
    {
      int from;
      int length;
      if (data.Contains("In"))
      {
        from = data.IndexOf('(') + 1;
        length = data.IndexOf(')') - data.IndexOf('(') - 1;
        return data.Substring(from, length).Replace("'", "");
      }
      else if (data.Contains("="))
      {
        from = data.IndexOf('=') + 1;
        length = data.Length - data.IndexOf('=') - 1;
        return data.Substring(from, length).Replace("'", "").Replace(" ", "");
      }
      else if (data.Contains("<>"))
      {
        from = data.IndexOf("<>") + 2;
        length = data.Length - data.IndexOf("<>") - 2;
        return data.Substring(from, length).Replace("'", "").Replace(" ", "");
      }
      else if (data.Contains("Like"))
      {
        from = data.IndexOf("Like") + 5;
        length = data.Length - data.IndexOf("Like") - 5;
        return data.Substring(from, length).Replace("'", "").Replace(" ", "").Replace("%", "");
      }
      else
      {
        return string.Empty;
      }

    }
    #endregion

    #region Обработчики TreeList
    private void tLSapClassifier_AfterCheckNode(object sender, NodeEventArgs e)
    {
      if (e.Node.CheckState == CheckState.Indeterminate && sender != null)
        e.Node.CheckState = CheckState.Checked;

      tLSapClassifier.BeginUpdate();
      try
      {
        SetCheckedChildNodes(e.Node, e.Node.CheckState);

        if (e.Node.ParentNode != null)
          SetCheckedParentNodes(e.Node.ParentNode, e.Node.CheckState);
      }
      finally
      {
        tLSapClassifier.EndUpdate();
      }
    }

    private void tLSapClassifier_NodeChanged(object sender, DevExpress.XtraTreeList.NodeChangedEventArgs e)
    {
      if (e.ChangeType == DevExpress.XtraTreeList.NodeChangeTypeEnum.Add)
        tLSapClassifier.FocusedNode = tLSapClassifier.Nodes[0];
    }

    private void SetCheckedChildNodes(TreeListNode node, CheckState checkState)
    {
      for (int i = 0; i < node.Nodes.Count; i++)
      {
        node.Nodes[i].CheckState = checkState;
        SetCheckedChildNodes(node.Nodes[i], checkState);
      }
    }

    private void SetCheckedParentNodes(TreeListNode node, CheckState checkState)
    {
      bool b = false;

      for (int i = 0; i < node.Nodes.Count; i++)
      {
        if (!checkState.Equals(node.Nodes[i].CheckState))
        {
          b = !b;
          break;
        }
      }

      node.CheckState = b ? CheckState.Indeterminate : checkState;

      if (node.ParentNode != null)
        SetCheckedParentNodes(node.ParentNode, checkState);
    }

    public static List<int> GetChildNodes(TreeListNode node)
    {
      List<int> list = new List<int>();

      if (node.HasChildren)
        for (int i = 0; i < node.Nodes.Count; i++)
          list.AddRange(GetChildNodes(node.Nodes[i]));
      else
        if (node.CheckState == CheckState.Checked)
          list.Add(int.Parse(node.GetValue(Fields.classifierId).ToString()));

      return list;
    }

    public static List<string> GetChildNodesNames(TreeListNode node)
    {
      List<string> list = new List<string>();

      if (node.HasChildren)
        for (int i = 0; i < node.Nodes.Count; i++)
          list.AddRange(GetChildNodesNames(node.Nodes[i]));
      else
        if (node.CheckState == CheckState.Checked)
          list.Add(node.GetValue(Fields.classifierName).ToString());

      return list;
    }

    public void ClearChildNodes(TreeListNode node)
    {
      if (node.HasChildren)
      {
        if (node.CheckState != CheckState.Unchecked)
          node.CheckState = CheckState.Unchecked;
        for (int i = 0; i < node.Nodes.Count; i++)
          ClearChildNodes(node.Nodes[i]);
      }
      else
      {
        if (node.CheckState != CheckState.Unchecked)
          node.CheckState = CheckState.Unchecked;
      }
    }

    public void SetChildNodes(TreeListNode node, string[] data)
    {
      if (node.HasChildren)
        for (int i = 0; i < node.Nodes.Count; i++)
          SetChildNodes(node.Nodes[i], data);
      else
      {
        if (data.Contains(node.GetValue(Fields.classifierId).ToString()))
          if (node.CheckState == CheckState.Unchecked)
          {
            node.CheckState = CheckState.Checked;
            if (node.ParentNode != null)
              SetCheckedParentNodes(node.ParentNode, CheckState.Checked);
          }
      }
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

      layoutControlGrid.Items.ConvertToTypedList().ForEach(delegate(BaseLayoutItem item)
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
      layoutControlGrid.Items.ConvertToTypedList().ForEach(delegate(BaseLayoutItem item)
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
        Filter request = new Filter();

        if (rGTypeFilter.SelectedIndex == 0)
        { // Применяем фильтр на основе критериев отбора
          request = AssembleGoodsFilter();
        }
        else
        { // Применяем значения вставленые из буфера
          request = AssembleGoodsFilterFromClipboard();
          if (ceUseFilter.Checked)
            request.Add(AssembleGoodsFilter());
        }

        if (ReferenceEquals(request, null) ? true : request.IsEmpty)
          throw new Exception("Нет данных для отбора товара!");

        request.Add(new Filter(String.Format("[{0}] =  0", Fields.lagerQuality)));

        List<Dimension> dimensions = new List<Dimension>();

        if (rgImportFld.Enabled && (rgImportFld.EditValue.ToString() == "3"))
          dimensions.Add(new Dimension()
          {
            columns = new string[] { Fields.lagerId, Fields.lagerName, Fields.lagerUnit, Fields.lagerUnitTypeName, Fields.lagerUnitQuantity, Fields.barcode },
            name = Dims.dim_Lagers,
            operationNames = new string[] { operationName },
            expression = request.Assemble(Filter.AssembleType.Sql),
            resultName = "dataGoods",
            orderBy = new OrderBy[] { new OrderBy() { Value = Fields.lagerName } }
          });
        else
          dimensions.Add(new Dimension()
          {
            columns = new string[] { Fields.lagerId, Fields.lagerName, Fields.lagerUnit, Fields.lagerUnitTypeName, Fields.lagerUnitQuantity},
            name = Dims.dim_Lagers,
            operationNames = new string[] { operationName },
            expression = request.Assemble(Filter.AssembleType.Sql),
            resultName = "dataGoods",
            orderBy = new OrderBy[] { new OrderBy() { Value = Fields.lagerName } }
          });


        goodsFilter = request;
        goodsRequest = request.Assemble(Filter.AssembleType.Sql);
        AddChaseControlOnSerch();
        FZCoreProxy.GetMasterDataAsyncStreamed(null, GetGoodsDataCallBack, new MasterDataRequest()
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
      DefaultMessageContract c = o as DefaultMessageContract;
      try
      {
        if (c == null || c.errorCode != ErrorCodes.OK)
        {
          throw new Exception((o as IDefaultContract).ToString());
        }

        IDataReader reader = c.GetDataReader();
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
            object[] values = new object[reader.FieldCount];
            reader.GetValues(values);

            DataRow row = table.NewRow();
            row.ItemArray = values;
            table.Rows.Add(row);
          }
          if (ThGoods.SelectedTabPage != TGoodsFilterGrid)
            ThGoods.SelectedTabPage = TGoodsFilterGrid;

          if (!table.Columns.Contains("AplyGoods"))
          {
            table.Columns.Add("AplyGoods", typeof(bool));
          }
          // Присваиваем значение по умолчанию для поля штучного выбора артикулов
          foreach (DataRow row in table.Rows)
            row["AplyGoods"] = "true";

          if ((cEClearData.Checked) || (goodsGrid.DataSource == null))
            goodsGrid.DataSource = table;
          else
          {
            DataTable TmpStore = (goodsGrid.DataSource as DataTable);
            TmpStore.PrimaryKey = new DataColumn[] { TmpStore.Columns[0] };
            //TmpStore.Load(table.CreateDataReader(), LoadOption.Upsert);
            TmpStore.Merge(table);
            goodsGrid.DataSource = TmpStore;
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

    #region Обработчики грида выбора артикулов

    /// <summary>
    /// В гриде отбора артикулов ставим/убераем выбор артикула по двойному клику
    /// </summary>
    private void goodsGrid_DoubleClick(object sender, EventArgs e)
    { // если грид пустой, то ничего не делаем
      if (goodsGridView.DataRowCount == 0)
        return;

      if (goodsGridView.GetRowCellValue(goodsGridView.FocusedRowHandle, gCChecked).ToString() == "True")
      { // Артикул выбран - убираем выбор
        goodsGridView.SetFocusedRowCellValue("AplyGoods", "False");
      }
      else
      { // Артикул не выбран - выбераем
        goodsGridView.SetFocusedRowCellValue("AplyGoods", "True");
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
          object o = (item as LayoutControlItem).Control;

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
      isChecked = true;
      isClearData = true;
      isFromBuffer = false;
    }
    #endregion

    #region Обработка нажатия княпок

    private void bOk_Click(object sender, EventArgs e)
    {
      Filter ExecutedFilter = new Filter(GroupOperatorType.And);
      _filterHint.Clear();

      try
      {
        isChecked = goodsGrid.DataSource == null ?
                      true :
                      (goodsGrid.DataSource as DataTable).Select("AplyGoods = False").Count() > 0 ?
                        false :
                        true;

        List<int> dataList = new List<int>();
        if ((isClearData == false) || (isFromBuffer) || (!isChecked))
        {
          if (goodsGrid.DataSource == null ? true : (goodsGrid.DataSource as DataTable).Select("AplyGoods = True").Count() == 0 ? true : false)
            throw new Exception("Нет выбраных товаров для отбора!");

          foreach (DataRow row in (goodsGrid.DataSource as DataTable).Rows)
            if (row["AplyGoods"].ToString() == "True")
              dataList.Add(int.Parse(row["Lagers.lagerId"].ToString()));



          CriteriaOperator co = new InOperator(goodsGrid.Tag.ToString(), dataList);
          if (cEExcludeGoods.Checked) co = new NotOperator(co);
          ExecutedFilter.Add(co);
          ExecutedFilter.Add(new Filter(String.Format("[{0}] =  0", Fields.lagerQuality)));
          goodsFilter = ExecutedFilter;
          goodsRequest = ExecutedFilter.Assemble(Filter.AssembleType.Sql);
          goodsArray = dataList.ToArray();

          List<string> sl = dataList.ConvertAll<string>(delegate(int i)
          {
            return i.ToString();
          });

          if (sl.Count < 10)
          {
            _filterHint.Add(String.Format(cEExcludeGoods.Checked ? HintTemplates["lagerSetExclude"] : HintTemplates["lagerSet"], String.Join(", ", sl.ToArray())));
          }
          else
          {
            _filterHint.Add(String.Format(cEExcludeGoods.Checked ? HintTemplates["lagerSetLargeExclude"] : HintTemplates["lagerSetLarge"], sl.Count));
          }


          DialogResult = DialogResult.OK;
          //dataList.ConvertAll<string>(delegate(int i){ return i.toString(); })
        }
        else
        {
          if (string.IsNullOrEmpty(goodsRequest))
          {
            ExecutedFilter = AssembleGoodsFilter();
            if (ReferenceEquals(ExecutedFilter, null) ? true : ExecutedFilter.IsEmpty)
            {
              goodsRequest = String.Empty;
            }
            else
            {
              ExecutedFilter.Add(new Filter(String.Format("[{0}] =  0", Fields.lagerQuality)));
              goodsRequest = ExecutedFilter.Assemble(Filter.AssembleType.Sql);
            }
            this.DialogResult = DialogResult.OK;
          }
          else
          {
            this.DialogResult = DialogResult.OK;
          }
        }
      }
      catch (Exception ex)
      {
        MB.error(Ex.Message(ex));
      }
    }

    private void BClearData_Click(object sender, EventArgs e)
    {
      ClearFilterAndData();
    }

    private void BGetData_Click(object sender, EventArgs e)
    {
      GetGoods();
      isClearData = cEClearData.Checked;
      isFromBuffer = rGTypeFilter.SelectedIndex == 0 ? false : true;
    }

    private void BCheckedAll_Click(object sender, EventArgs e)
    {
      // Проверяем не пуст ли грид отбора артикулов, есть ли в нем выбраные артикула
      if ((goodsGrid.DataSource == null ? false : (goodsGrid.DataSource as DataTable).Rows.Count > 0))
      { // Выбраных артикулов нет
        // Выбираем в гриде отбора артикулов все артикула
        foreach (DataRow row in (goodsGrid.DataSource as DataTable).Rows)
          row["AplyGoods"] = "true";
      }

    }

    private void BUnCheckedAll_Click(object sender, EventArgs e)
    {
      // Проверяем не пуст ли грид отбора артикулов, есть ли в нем выбраные артикула
      if ((goodsGrid.DataSource == null ? false : (goodsGrid.DataSource as DataTable).Rows.Count > 0))
      { // Выбраных артикулов нет
        // Отменяем в гриде отбора артикулов все артикула
        foreach (DataRow row in (goodsGrid.DataSource as DataTable).Rows)
          row["AplyGoods"] = "false";
      }
    }
    private void rGTypeFilter_SelectedIndexChanged(object sender, EventArgs e)
    {
      rgImportFld.Enabled = rGTypeFilter.SelectedIndex == 1;
      ceUseFilter.Enabled = rgImportFld.Enabled;
    }

    private void ELagerId_ButtonClick(object sender, ButtonPressedEventArgs e)
    {
      if (e.Button.Kind == ButtonPredefines.Ellipsis)
      {
        using (LagersList form = new LagersList())
        {
          form.lagerIds = goodsArray;
          if (form.ShowDialog(this) == DialogResult.OK)
          {
            goodsArray = form.lagerIds;
            ELagerId.Properties.Mask.MaskType = MaskType.None;
            ELagerId.Text = "набор";
            ELagerId.Properties.ReadOnly = true;
            foreach (EditorButton eb in ELagerId.Properties.Buttons)
              if (eb.Kind == ButtonPredefines.Delete) eb.Visible = true;
          }
        }
      }
      else if (e.Button.Kind == ButtonPredefines.Delete)
      {
        goodsArray = new int[0];
        ELagerId.Text = "";
        ELagerId.Properties.Mask.MaskType = MaskType.RegEx;
        ELagerId.Properties.ReadOnly = false;
        e.Button.Visible = false;
      }
    }

    private void rgImportFld_Properties_EditValueChanging(object sender, ChangingEventArgs e)
    {
      gCBarcode.VisibleIndex = e.NewValue.ToString() == "3" ? 0 : -1;
    }

    #endregion


  }
}