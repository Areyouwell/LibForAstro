using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zeptomoby.OrbitTools;

namespace Windage
{
    public class ResistanceFactor
    {
        public static Dictionary<DateTime, double> AtmosphericResistanceCoefficient(List<List<string>> SortedTle)
        {
            Dictionary<DateTime, double> alphaInTime = new Dictionary<DateTime, double>();
            for (int i = 0; i < SortedTle.Count - 1; i++)
            {
                Tle tle1 = new Tle("", SortedTle[i][0], SortedTle[i][1]);
                Tle tle2 = new Tle("", SortedTle[i + 1][0], SortedTle[i + 1][1]);
                Satellite sat1 = new Satellite(tle1);
                Satellite sat2 = new Satellite(tle2);
                Orbit orb1 = new Orbit(tle1);
                DateTime dayOfYear = ConvertNumberToDate(tle1.Epoch);
                double numOfRev = double.Parse(tle1.MeanMotion.Substring(0, 11).Replace('.', ','));
                var vAp1AndVPer1 = GetVelOfApAndPer(sat1, numOfRev, dayOfYear);
                var vAp2AndVPer2 = GetVelOfApAndPer(sat2, numOfRev, dayOfYear);
                double vAp1 = vAp1AndVPer1.Item1;
                double vAp2 = vAp2AndVPer2.Item1;
                double vPer1 = vAp1AndVPer1.Item2;
                double vPer2 = vAp2AndVPer2.Item2;
                double alphaApogee = CalculateCoeff(vAp1, vAp2, numOfRev, orb1.Apogee, orb1.Perigee, orb1.Eccentricity);
                double alphaPerigee = CalculateCoeff(vPer1, vPer2, numOfRev, orb1.Apogee, orb1.Perigee, orb1.Eccentricity);
                alphaInTime.Add(dayOfYear, (alphaApogee + alphaPerigee) / 2);
            }
            return alphaInTime;
        }   

        private static (double, double) GetVelOfApAndPer(Satellite sat, double numOfRev, DateTime dayOfYear)
        {
            int timeInterval = (int)Math.Round(24 * 120 / numOfRev);
            Dictionary<DateTime, double> radVecOfTime = GetRadiusVecOfTime(sat, dayOfYear, timeInterval);
            var VelosOfApAndPer = LookForApoAndPerVelos(sat, radVecOfTime);
            double vAp = VelosOfApAndPer.Item1;
            double vPer = VelosOfApAndPer.Item2;
            return (vAp, vPer);
        }

        private static Dictionary<DateTime, double> GetRadiusVecOfTime(Satellite sat, DateTime dayOfYear, int timeInterval)
        {
            Dictionary<DateTime, double> radVecOfTime = new Dictionary<DateTime, double>();
            for (int j = 0; j < timeInterval; j++)
            {
                Eci eci = sat.PositionEci(dayOfYear);
                double radVec = ModuleRadius(eci);
                radVecOfTime.Add(dayOfYear, radVec);
                dayOfYear = dayOfYear.AddSeconds(30);
            }
            return radVecOfTime;
        }

        private static (double, double) LookForApoAndPerVelos(Satellite sat, Dictionary<DateTime, double> radVecOfTime)
        {
            DateTime timeOfApogee = new DateTime();
            DateTime timeOfPerigee = new DateTime();
            double radApogee = radVecOfTime.Values.Max();
            double radPerigee = radVecOfTime.Values.Min();
            foreach (var item in radVecOfTime)
            {
                if (timeOfApogee != new DateTime() && timeOfPerigee != new DateTime()) break;
                if (item.Value == radApogee)
                {
                    timeOfApogee = item.Key;
                }
                if (item.Value == radPerigee)
                {
                    timeOfPerigee = item.Key;
                }
            }
            Eci eci1 = sat.PositionEci(timeOfApogee);
            Eci eci2 = sat.PositionEci(timeOfPerigee);
            double vAp = ModuleVelos(eci1);
            double vPer = ModuleVelos(eci2);
            return (vAp, vPer);
        }

        private static double ModuleVelos(Eci eci)
        {
            return Math.Sqrt(eci.Velocity.X * eci.Velocity.X + eci.Velocity.Y * eci.Velocity.Y
                + eci.Velocity.Z * eci.Velocity.Z);
        }

        private static double ModuleRadius(Eci eci)
        {
            return Math.Sqrt(eci.Position.X * eci.Position.X + eci.Position.Y * eci.Position.Y +
                    eci.Position.Z * eci.Position.Z);
        }

        //Подставляем в формулу
        private static double CalculateCoeff(double v1, double v2, double numOfRev, double apogee,
                                                    double perigee, double eccent)
        {
            int weightSatel = 1900; // масса ступника
            return Math.Abs(weightSatel * (v2 * v2 - v1 * v1) / (2 * numOfRev * v1 * v1
                * CalcLenEllipse(apogee, perigee, eccent)));
        }

        //Перевод количества дней в дату
        public static DateTime ConvertNumberToDate(string number)
        {
            DateTime date1 = new DateTime();
            int year = int.Parse("20" + number.Substring(0, 2));
            int day = int.Parse(number.Substring(2, 3));
            Dictionary<int, int> AllMonth = CreateCalendar(year);
            int checkNum = day;
            for (int i = 1; i < 13; i++)
            {
                checkNum -= AllMonth[i];
                if (checkNum <= 0)
                {
                    date1 = new DateTime(year, i, checkNum + AllMonth[i]);
                    break;
                }
            }
            return date1;
        }

        //Создаем календарь
        private static Dictionary<int, int> CreateCalendar(int year)
        {
            Dictionary<int, int> AllMonth = new Dictionary<int, int>();
            for (int i = 1; i < 13; i++)
                AllMonth.Add(i, 31);
            AllMonth[4] = 30;
            AllMonth[6] = 30;
            AllMonth[9] = 30;
            AllMonth[11] = 30;
            if (DateTime.IsLeapYear(year))
                AllMonth[2] = 29;
            else
                AllMonth[2] = 28;
            return AllMonth;
        }

        //Считывает Tle из файла и сохраняем в список
        public static List<List<string>> ReadTleFromFile(string path)
        {
            List<List<string>> AllTle = new List<List<string>>();
            StreamReader sr = new StreamReader(path, System.Text.Encoding.Default);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                List<string> OneTle = new List<string>();
                OneTle.Add(line);
                OneTle.Add(sr.ReadLine());
                AllTle.Add(OneTle);
            }
            return AllTle;
        }

        //Выбираем нужные Tle координаты из заданного временного промежутка
        public static List<List<string>> ChoiseTle(List<List<string>> AllTle, DateTime date1, DateTime date2)
        {
            List<List<string>> SortedTle = new List<List<string>>();
            if (ConvertNumberToDate(AllTle[0][0].Substring(18, 5)) == date1)
                SortedTle.Add(AllTle[0]); //костыль, чтобы добавить первую дату
            for (int i = 1; i < AllTle.Count; i++)
            {
                DateTime dateCur = ConvertNumberToDate(AllTle[i][0].Substring(18, 5));
                if (dateCur < date1)
                    continue;
                if (dateCur > date2.AddDays(1))
                    break;
                DateTime datePast = ConvertNumberToDate(AllTle[i - 1][0].Substring(18, 5));
                if (dateCur >= date1 && dateCur <= date2.AddDays(1) && dateCur != datePast) //проверяем чтобы дата
                    SortedTle.Add(AllTle[i]); //была в нужном временном промежутке и не было повторяющихся дат
            }
            return SortedTle;
        }

        //Вычисляем длину орбиты с заданными апогеем, перигеем и эксцентриситетом
        private static double CalcLenEllipse(double apogee, double perigee, double eccent)
        {
            int radEarth = 6371;
            double a = (apogee + perigee + 2 * radEarth) / 2;
            double b = a * Math.Sqrt(1 - eccent * eccent);
            return (4 * (Math.PI * a * b + (a - b) * (a - b)) / (a + b));
        }

        //GetRadiusFull
        public static Dictionary<DateTime, double> GetRadiusFull(List<List<string>> SortedTle)
        {
            Dictionary<DateTime, double> radiusInTime = new Dictionary<DateTime, double>();
            for (int i = 0; i < SortedTle.Count - 1; i++)
            {
                Tle tle1 = new Tle("", SortedTle[i][0], SortedTle[i][1]);
                Satellite sat1 = new Satellite(tle1);
                DateTime dayOfYear = ConvertNumberToDate(tle1.Epoch);
                //1440 минут в 24 часах, соответственно если мы получаем радиус каждые 2 минуты, то цикл выполняется 720
                for (int j = 0; j < 720; j++)
                {
                    Eci eci = sat1.PositionEci(dayOfYear);
                    radiusInTime.Add(dayOfYear, ModuleRadius(eci));
                    dayOfYear = dayOfYear.AddMinutes(2);
                }
            }
            return radiusInTime;
        }
    }
}
