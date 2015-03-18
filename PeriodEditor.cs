using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using FozzySystems.Utils;

namespace FZComponents.Dialogs
{
  public partial class PeriodEditor : XtraForm
  {
    public class Period
    {
      public DateTime dateFrom;
      public DateTime dateTo;
      public DayOfWeek[] daysOfWeek;
      public TimeSpan timeFrom;
      public TimeSpan timeTo;

      public class PeriodException : Exception
      {
        public PeriodException(string message) : base(message) { }
      }

      public static string Join(string[] periods)
      {
        return String.Join("|", periods);
      }

      public static string[] Split(string period)
      {
        return period.Split('|');
      }

      public string[] ToStringArray()
      {
        List<string> period = new List<string>();

        /*Дата*/
        if (dateFrom.Date.CompareTo(dateTo.Date) > 0)
          throw new PeriodException("Начальная дата должна быть меньше конечной даты.");

        string date;

        if (dateFrom.Year == dateTo.Year)
          if (dateFrom.Month == dateTo.Month)
            date = String.Format("{0:yyyy:M:d}-{1}", dateFrom, dateTo.Day);
          else
            date = String.Format("{0:yyyy:(M:d})-({1:M:d})", dateFrom, dateTo);
        else
          date = String.Format("({0:yyyy:M:d})-({1:yyyy:M:d})", dateFrom, dateTo);

        /*Время*/
        int timeCompare = timeFrom.CompareTo(timeTo);
        bool endTimeIsZero = (timeTo.CompareTo(DateTime.MinValue.TimeOfDay) == 0);

        int slots = (timeCompare == 1) ? 2 : 1;
        for (int slot = 0; slot < slots; slot++)
        {
          string hours = String.Empty;

          if (timeCompare == 0 && !endTimeIsZero)
            hours = timeFrom.ToString("hh\\:mm");
          else if (timeCompare < 0)
            hours = String.Format("({0:hh\\:mm})-({1:hh\\:mm})", timeFrom, timeTo);
          else if (timeCompare > 0)
          {
            if (slot == 0)
              hours = String.Format("({0:hh\\:mm})-(23:59)", timeFrom);
            else if (!endTimeIsZero)
              hours = String.Format("(00:00)-({0:hh\\:mm})", timeTo);
            else
              break;
          }

          /*Дни недели*/
          string[] codes = { "SU", "MO", "TU", "WE", "TH", "FR", "SA" };
          var dayOfWeek = new List<string>();
          foreach (var d in daysOfWeek)
            dayOfWeek.Add(codes[(int)d]);
          if (dayOfWeek.Count == 7)
            dayOfWeek.Clear();

          period.Add(String.Format("{0}:{1}:{2}", date, String.Join(",", dayOfWeek.ToArray()), hours));
        }

        return period.ToArray();
      }

      public static Period[] Parse(string period)
      {
        Period[] periodParts = new Period[0];

        try
        {
          if (String.IsNullOrEmpty(period))
            return periodParts;

          //Устанавливаем день недели в числовое значение.
          period = period.Replace("MO", "1");
          period = period.Replace("TU", "2");
          period = period.Replace("WE", "3");
          period = period.Replace("TH", "4");
          period = period.Replace("FR", "5");
          period = period.Replace("SA", "6");
          period = period.Replace("SU", "0");

          //разбиваем на периоды (по '|')
          string[] periods = period.Split('|');

          periodParts = new Period[periods.Length];

          for (int i = 0; i < periods.Length; i++)
          {
            periodParts[i] = new Period();

            //меняем делимитер у группированных периодах на ';' : (10:*:01) => (10;*;01)
            var groupMask = new Regex(@"\(([^)]+):([^)]+)\)");

            while (groupMask.IsMatch(periods[i]))
            {
              Match m = groupMask.Match(periods[i]);
              periods[i] = periods[i].Replace(m.Value, m.Value.Replace(':', ';'));
            }

            //разбиваем на поля (по ':')
            string[] entries = periods[i].Split(':');

            if (entries.Length == 6)
            {
              entries[4] = String.Format("{0}:{1}", entries[4], entries[5]);
              entries = entries.Take(5).ToArray();
            }

            // Дата
            int yearFrom, yearTo, monthFrom, monthTo, dayFrom, dayTo;
            string[] dateValues = entries[entries.Length - 3].Split(new char[] { '(', ')', '-' }, StringSplitOptions.RemoveEmptyEntries);
            Func<string, int[]> getDateParts = (dateValue) => dateValue.Split(';').Select(s => int.Parse(s)).Reverse().ToArray();
            int[] datePartsFrom = getDateParts(dateValues[0]);
            int[] datePartsTo = getDateParts(dateValues[1]);

            dayFrom = datePartsFrom[0];
            dayTo = datePartsTo[0];

            if (entries.Length == 3 || entries.Length == 4)
            {
              monthFrom = datePartsFrom[1];
              monthTo = datePartsTo[1];
            }
            else
              monthFrom = monthTo = int.Parse(entries[1]);

            if (entries.Length == 3)
            {
              yearFrom = datePartsFrom[2];
              yearTo = datePartsTo[2];
            }
            else
              yearFrom = yearTo = int.Parse(entries[0]);

            periodParts[i].dateFrom = new DateTime(yearFrom, monthFrom, dayFrom);
            periodParts[i].dateTo = new DateTime(yearTo, monthTo, dayTo);

            // Дни недели
            periodParts[i].daysOfWeek = String.IsNullOrEmpty(entries[entries.Length - 2]) ? new DayOfWeek[0] :
              entries[entries.Length - 2].Split(',').Select(s => (DayOfWeek)int.Parse(s)).ToArray();

            // Время
            if (!String.IsNullOrEmpty(entries[entries.Length - 1]))
            {
              string[] timeValues = entries[entries.Length - 1].Replace(';', ':').Split(new char[] { '(', ')', '-' }, StringSplitOptions.RemoveEmptyEntries);
              periodParts[i].timeFrom = TimeSpan.ParseExact(timeValues[0], "hh\\:mm", CultureInfo.InvariantCulture);
              if (timeValues.Length == 1)
                periodParts[i].timeTo = periodParts[i].timeFrom;
              else
                periodParts[i].timeTo = TimeSpan.ParseExact(timeValues[1], "hh\\:mm", CultureInfo.InvariantCulture);
            }
          }
        }
        catch (PeriodException ex)
        {
          MB.error(ex);
        }

        return periodParts;
      }

      public static bool IsDateInPeriod(string period, DateTime date)
      {
        if (String.IsNullOrEmpty(period))
          return false;
        //Устанавливаем день недели в числовое значение.
        period = period.Replace("MO", "1");
        period = period.Replace("TU", "2");
        period = period.Replace("WE", "3");
        period = period.Replace("TH", "4");
        period = period.Replace("FR", "5");
        period = period.Replace("SA", "6");
        period = period.Replace("SU", "7");
        //разбиваем на периоды (по '|')
        string[] periods = period.Split('|');

        for (int i = 0; i < periods.Length; i++)
        {
          //YYYY:MM:DD:DAYOFWEEK:HH:MM:SS
          string currentDate = date.ToString("yyyy:MM:dd:") + ((int)date.DayOfWeek).ToString() + date.ToString(":HH:mm:ss");

          //меняем делимитер у группированных периодах на ';' : (10:*:01) => (10;*;01)
          var groupMask = new Regex(@"\(([^)]+):([^)]+)\)");

          while (groupMask.IsMatch(periods[i]))
          {
            Match m = groupMask.Match(periods[i]);
            periods[i] = periods[i].Replace(m.Value, m.Value.Replace(':', ';'));
          }
          //разбиваем на поля (по ':')
          string[] entries = periods[i].Split(':');

          bool matched = true;

          for (int j = 0; j < entries.Length; j++)
          {
            int current = Int32.Parse(currentDate.Substring(0, currentDate.IndexOf(':')));
            currentDate = currentDate.Remove(0, currentDate.IndexOf(':') + 1);
            //пропускаем полные периоды.
            if (String.IsNullOrEmpty(entries[j]) || entries[j] == "*")
              continue;

            if (!entries[j].Contains("("))
            {
              matched = false;
              foreach (string ss in entries[j].Split(','))
              {
                int from, to;
                if (!ss.Contains("-"))
                  from = to = Int32.Parse(ss);
                else
                {
                  from = Int32.Parse(ss.Substring(0, ss.IndexOf('-')));
                  to = Int32.Parse(ss.Substring(ss.IndexOf('-') + 1, ss.Length - ss.IndexOf('-') - 1));
                }
                if (from <= current && to >= current)
                {
                  matched = true;
                  break;
                }
              }
            }
            else
            {
              matched = false;
              //Проверка на синтаксис субпериода. Должен удовлетворять синтаксису:
              //(n[;n...]])-(n[;n...]])
              //Если синтаксис не верен, пропускаем текущий период.
              var subperiodMask = new Regex(@"\(([^)]+)\)-\(([^)]+)\)");
              if (!subperiodMask.IsMatch(entries[j]))
                break;
              //парсинг диапазона - "(x)-(y)".
              //Диапазон указывает полный период времени от начальной даты до конечной.
              Match m = subperiodMask.Match(entries[j]);
              string[] p1 = m.Groups[1].Value.Split(';');
              string[] p2 = m.Groups[2].Value.Split(';');
              //Проверка на количество полей.
              if (p1.Length != p2.Length)
                break;
              //Восстанавливаем последнее значение currentDate (для упрощения цикла).
              currentDate = current.ToString() + ":" + currentDate;
              matched = true;
              bool skip_from = false, skip_to = false;
              for (int k = 0; k < p1.Length; k++)
              {
                current = Int32.Parse(currentDate.Substring(0, currentDate.IndexOf(':')));
                currentDate = currentDate.Remove(0, currentDate.IndexOf(':') + 1);
                int @from = (skip_from || String.IsNullOrEmpty(p1[k]) || p1[k] == "*") ? current : Int32.Parse(p1[k]);
                int to = (skip_to || String.IsNullOrEmpty(p2[k]) || p2[k] == "*") ? current : Int32.Parse(p2[k]);
                if (from > current || to < current)
                {
                  matched = false;
                  break;
                }
                if (from < current)
                  skip_from = true;

                if (to > current)
                  skip_to = true;
              }
            }
            if (!matched)
              break;
          }
          if (matched)
            return true;
        }
        return false;
      }
    }

    public string[] PeriodStringArray
    {
      get
      {
        return lbcPeriod.Items.Cast<string>().ToArray();
      }
      set
      {
        lbcPeriod.Items.Clear();
        lbcPeriod.Items.AddRange(value);
      }
    }

    public string PeriodString
    {
      get
      {
        return Period.Join(PeriodStringArray);
      }
      set
      {
        PeriodStringArray = Period.Split(value);
      }
    }

    public Period[] PeriodDetails { get { return Period.Parse(PeriodString); } }

    public int PeriodItemsLimit { get; set; }

    public bool UserConfirmations { get; set; }

    public PeriodEditor()
    {
      InitializeComponent();

      ceMO.Tag = DayOfWeek.Monday;
      ceTU.Tag = DayOfWeek.Tuesday;
      ceWE.Tag = DayOfWeek.Wednesday;
      ceTH.Tag = DayOfWeek.Thursday;
      ceFR.Tag = DayOfWeek.Friday;
      ceSA.Tag = DayOfWeek.Saturday;
      ceSU.Tag = DayOfWeek.Sunday;
    }

    private void PeriodEditor_Shown(object sender, EventArgs e)
    {
      deStartDate.Properties.MinValue = DateTime.Today;
      deEndDate.Properties.MinValue = DateTime.Today;
      deStartDate.DateTime = DateTime.Today;
      deEndDate.DateTime = DateTime.Today;
      deCheckDate.DateTime = DateTime.Now;
    }

    private void sbDelete_Click(object sender, EventArgs e)
    {
      if (lbcPeriod.SelectedIndex != -1)
        lbcPeriod.Items.RemoveAt(lbcPeriod.SelectedIndex);
    }

    private void sbAdd_Click(object sender, EventArgs e)
    {
      try
      {
        if (lbcPeriod.Items.Count == PeriodItemsLimit)
        {
          MB.error(Text, "Невозможно добавить диапазон. Чтобы добавить новый диапазон, удалите существующий.");
          return;
        }

        /*Дни недели*/
        var dayOfWeek = new List<DayOfWeek>();
        for (int i = 0; i < lcgDayofWeek.Items.ItemCount; i++)
        {
          object o = lcgDayofWeek.Items[i];
          if (o is DevExpress.XtraLayout.LayoutControlItem)
            if ((o as DevExpress.XtraLayout.LayoutControlItem).Control is CheckEdit)
              if (((o as DevExpress.XtraLayout.LayoutControlItem).Control as CheckEdit).Checked)
                dayOfWeek.Add((DayOfWeek)((o as DevExpress.XtraLayout.LayoutControlItem).Control as CheckEdit).Tag);
        }

        Period period = new Period()
        {
          dateFrom = deStartDate.DateTime,
          dateTo = deEndDate.DateTime,
          timeFrom = TimeSpan.ParseExact(teStartTime.Text, "hh\\:mm", CultureInfo.InvariantCulture),
          timeTo = TimeSpan.ParseExact(teEndTime.Text, "hh\\:mm", CultureInfo.InvariantCulture),
          daysOfWeek = dayOfWeek.ToArray()
        };
        string[] periodStrings = period.ToStringArray();

        if (periodStrings.Length > 1 && lbcPeriod.Items.Count == PeriodItemsLimit - 1)
        {
          MB.error(Text, "Невозможно добавить диапазон. Чтобы добавить новый диапазон, удалите существующий.");
          return;
        }

        lbcPeriod.Items.AddRange(periodStrings);
      }
      catch (Exception ex)
      {
        MB.error(ex);
      }
    }

    private void sbCancel_Click(object sender, EventArgs e)
    {
      Close();
    }

    private void PeriodEditor_FormClosing(object sender, FormClosingEventArgs e)
    {
      if (DialogResult != DialogResult.OK)
      {
        if (!UserConfirmations || MB.warningYesNo(Text, "Выйти без сохранения периода?") == DialogResult.Yes)
          DialogResult = DialogResult.Cancel;
        else
          e.Cancel = true;
      }
    }

    private void sbApply_Click(object sender, EventArgs e)
    {
      if (!UserConfirmations || MB.questionYesNo(Text, "Принять изменения в периоде?") == DialogResult.Yes)
        DialogResult = DialogResult.OK;
    }

    private void sbCheckDate_Click(object sender, EventArgs e)
    {
      try
      {
        if (Period.IsDateInPeriod(PeriodString, deCheckDate.DateTime))
          MB.mb(Text, "Дата попадает в период.", new DialogResult[] { DialogResult.OK }, Properties.Resources.success);
        else
          MB.mb(Text, "Дата не попадает в период.", new DialogResult[] { DialogResult.OK }, Properties.Resources.fail);
      }
      catch (Exception ex)
      {
        MB.error(ex);
      }
    }
  }
}