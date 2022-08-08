using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zeptomoby.OrbitTools;
using Excel = Microsoft.Office.Interop.Excel;


namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime datee = new DateTime(2009, 01, 02);
            DateTime datee2 = new DateTime(2018, 09, 10);
            string path = @"D:\Lesson\programming\Си #\astro\tle 2009-2019.txt";
            
        }

        public static Dictionary<DateTime, double> GetCycloidPoints(Dictionary<DateTime, double> AllApogee)
        {
            Dictionary<DateTime, double> interestingCycloidPoints = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> meanCycloids = new Dictionary<DateTime, double>();
            List<double> tempCycloids = new List<double>();
            Dictionary<DateTime, double> meanCycloidsAllInterval = new Dictionary<DateTime, double>();
            bool valueUpOrDown = UpOrDown(AllApogee);
            int count = 0;
            double desiredPoint = 0;
            DateTime desiredDate = new DateTime();
            foreach (var item in AllApogee) //Находим точки максимума и минимума циклоиды
            {
                if (valueUpOrDown)
                {
                    if (count == 10)
                    {
                        interestingCycloidPoints.Add(desiredDate, desiredPoint);
                        count = 0;
                        valueUpOrDown = false;
                    }
                    if (item.Value > desiredPoint || desiredPoint == 0)
                    {
                        desiredPoint = item.Value;
                        desiredDate = item.Key;
                    }
                    if (item.Value < desiredPoint)
                        count++;
                }
                else
                {
                    if (count == 10)
                    {
                        interestingCycloidPoints.Add(desiredDate, desiredPoint);
                        count = 0;
                        valueUpOrDown = true;
                    }
                    if (item.Value < desiredPoint || desiredPoint == 0)
                    {
                        desiredPoint = item.Value;
                        desiredDate = item.Key;
                    }
                    if(item.Value > desiredPoint)
                        count++;
                }
            }
            var interCycPointValue = interestingCycloidPoints.Values.ToList();
            var interCycPointKey = interestingCycloidPoints.Keys.ToList();
            //Получаем средние значения в этих точках
            for (int i = 0; i < interCycPointValue.Count - 1; i++)
            {
                interCycPointValue[i] = (interCycPointValue[i] + interCycPointValue[i + 1]) / 2;
            }
            interCycPointValue[interCycPointValue.Count - 1] = interCycPointValue[interCycPointValue.Count - 2];
            for (int i = 0; i < interCycPointValue.Count; i++)
            {
                meanCycloids.Add(interCycPointKey[i], interCycPointValue[i]);
            }
            var meanCycloidsKeys = meanCycloids.Keys.ToList();
            var meanCycloidsValues = meanCycloids.Values.ToList();
            // Делаем значения плавными
            for (int i = 0; i < meanCycloids.Count - 1; i++)
            {
                int numberDay = (meanCycloidsKeys[i + 1] - meanCycloidsKeys[i]).Days;
                double differ = (meanCycloidsValues[i + 1] - meanCycloidsValues[i]) / (numberDay - 1);
                tempCycloids.Add(differ);
            }
            var AllApogeeKey = AllApogee.Keys.ToList();
            int lengthApogee = (AllApogeeKey[AllApogeeKey.Count - 1] - AllApogeeKey[0]).Days;
            DateTime allDayInInterval = AllApogeeKey[0];
            double tempCycloidsValues = meanCycloidsValues[0];
            for (int i = 0, j = 0; i < lengthApogee; i++)
            {  
                if (allDayInInterval.Date.CompareTo(meanCycloidsKeys[meanCycloidsKeys.Count - 1].Date) != 0 
                    && allDayInInterval.Date.CompareTo(meanCycloidsKeys[j + 1].Date) == 0)
                    j++;
                if (allDayInInterval.Date.CompareTo(meanCycloidsKeys[1].Date) > 0
                    && allDayInInterval.Date.CompareTo(meanCycloidsKeys[j].Date) > 0)
                    tempCycloidsValues += tempCycloids[j - 1];
                meanCycloidsAllInterval.Add(allDayInInterval, tempCycloidsValues);
                allDayInInterval = allDayInInterval.AddDays(1);
            }
            return meanCycloidsAllInterval;
        }

        public static bool UpOrDown(Dictionary<DateTime, double> AllApogee)
        {
            bool valueUpOrDown = true;
            int count = 0;
            double valueApogee = 0;
            foreach (var item in AllApogee)
            {
                if (item.Value > valueApogee)
                {
                    valueApogee = item.Value;
                    count++;
                }
                else
                    count--;
                if (count == 3)
                {
                    valueUpOrDown = true;
                    break;
                }
                if(count == -3)
                {
                    valueUpOrDown = false;
                    break;
                }
            }
            return valueUpOrDown;
        }

        public static Dictionary<DateTime, double> GetAverApoAndPer2(List<List<string>> SortedTle)
        {
            Dictionary<DateTime, double> middleRadiusInTime = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> PotentialEnergyInTime = new Dictionary<DateTime, double>();
            for (int i = 0, j = 0; i < SortedTle.Count - 1; i++, j++)
            {
                Tle tle = new Tle("", SortedTle[i][0], SortedTle[i][1]);
                Satellite sat = new Satellite(tle);
                DateTime dayOfYear = ConvertNumberToDate(tle.Epoch);
                double numOfRev = double.Parse(tle.MeanMotion.Substring(0, 11).Replace('.', ','));
                int timeInterval = (int)Math.Round(24 * 120 / numOfRev);
                while (j != 0 && middleRadiusInTime.Keys.ToList()[j - 1].AddDays(1) != dayOfYear)
                {
                    DateTime interestDate = middleRadiusInTime.Keys.ToList()[j - 1].AddDays(1);
                    Dictionary<DateTime, double> radVecOfTime1 = GetRadiusVecOfTime(sat, interestDate, timeInterval);
                    double mediumRadApoAndPer1 = (radVecOfTime1.Values.Max() + radVecOfTime1.Values.Min()) / 2;
                    middleRadiusInTime.Add(interestDate, mediumRadApoAndPer1);
                    j++;
                }
                Dictionary<DateTime, double> radVecOfTime = GetRadiusVecOfTime(sat, dayOfYear, timeInterval);
                double mediumRadApoAndPer = (radVecOfTime.Values.Max() + radVecOfTime.Values.Min()) / 2;
                middleRadiusInTime.Add(dayOfYear, mediumRadApoAndPer);
            }
            List<DateTime> dateee1 = middleRadiusInTime.Keys.ToList();
            List<double> radius11 = middleRadiusInTime.Values.ToList();
            const double mMG = 756856906000000;
            for (int i = 0; i < middleRadiusInTime.Count - 1; i++)
            {
                double r1 = radius11[i];
                double r2 = radius11[i + 1];
                double energy = mMG * Math.Abs((1 / r2) - (1 / r1));
                PotentialEnergyInTime.Add(dateee1[i], energy);
            }
            return PotentialEnergyInTime;
        }

        public static Dictionary<DateTime, double> GetAverApoAndPer(List<List<string>> SortedTle)
        {
            Dictionary<DateTime, double> middleRadiusInTime = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> PotentialEnergyInTime = new Dictionary<DateTime, double>();
            for (int i = 0; i < SortedTle.Count - 1; i++)
            {
                Tle tle = new Tle("", SortedTle[i][0], SortedTle[i][1]);
                Satellite sat = new Satellite(tle);
                DateTime dayOfYear = ConvertNumberToDate(tle.Epoch);
                double numOfRev = double.Parse(tle.MeanMotion.Substring(0, 11).Replace('.', ','));
                int timeInterval = (int)Math.Round(24 * 120 / numOfRev);
                Dictionary<DateTime, double> radVecOfTime = GetRadiusVecOfTime(sat, dayOfYear, timeInterval);
                double radApogee = radVecOfTime.Values.Max();
                double radPerigee = radVecOfTime.Values.Min();
                double mediumRadApoAndPer = (radApogee + radPerigee) / 2;
                middleRadiusInTime.Add(dayOfYear, mediumRadApoAndPer);
            }
            List<DateTime> dateee1 = middleRadiusInTime.Keys.ToList();
            List<double> radius11 = middleRadiusInTime.Values.ToList();
            const double mMG = 756856906000000000;
            for (int i = 0; i < middleRadiusInTime.Count - 1; i++)
            {
                double r1 = radius11[i] * 1000;
                double r2 = radius11[i + 1] * 1000;
                double energy = mMG * Math.Abs((1 / r2) - (1 / r1));
                PotentialEnergyInTime.Add(dateee1[i], energy);
            }
            return PotentialEnergyInTime;
        }

        public static Dictionary<DateTime, double> GetAplpha(List<List<string>> SortedTle)
        {
            Dictionary<DateTime, double> alphaInTime = new Dictionary<DateTime, double>();
            var AllApandPer = GetDicApogeeAndPerigee(SortedTle);
            var Arrapogee = AllApandPer.Item1.Values.ToList();
            var Arrperigee = AllApandPer.Item2.Values.ToList();
            for (int i = 0; i < SortedTle.Count - 1; i++)
            {
                Tle tle1 = new Tle("", SortedTle[i][0], SortedTle[i][1]);
                Tle tle2 = new Tle("", SortedTle[i + 1][0], SortedTle[i + 1][1]);
                Satellite sat1 = new Satellite(tle1);
                Satellite sat2 = new Satellite(tle2);
                Orbit orb1 = new Orbit(tle1);
                double numOfRev1 = double.Parse(tle1.MeanMotion.Substring(0, 11).Replace('.', ','));

                //double alphaApogee = CalculateForsePotential(Arrapogee[i], Arrapogee[i + 1], numOfRev1, orb1.Apogee, orb1.Perigee, orb1.Eccentricity);
                //double alphaPerigee = CalculateForsePotential(Arrperigee[i], Arrperigee[i + 1], numOfRev1, orb1.Apogee, orb1.Perigee, orb1.Eccentricity);
                //double vAp1 = GetVelosit(sat1, Arrapogee[i]);
                //double vAp2 = GetVelosit(sat2, Arrapogee[i + 1]);
                //double vPer1 = GetVelosit(sat1, Arrperigee[i]);
                //double vPer2 = GetVelosit(sat2, Arrperigee[i + 1]);
                //double alphaApogee = CalculateAlphaKinetic(vAp1, vAp2, numOfRev1, orb1.Apogee, orb1.Perigee, orb1.Eccentricity);
                //double alphaPerigee = CalculateAlphaKinetic(vPer1, vPer2, numOfRev1, orb1.Apogee, orb1.Perigee, orb1.Eccentricity);
                double alphaApogee = CalculatePotential(Arrapogee[i], Arrapogee[i + 1]);
                double alphaPerigee = CalculatePotential(Arrperigee[i], Arrperigee[i + 1]);

                alphaInTime.Add(ConvertNumberToDate(tle1.Epoch), (alphaApogee + alphaPerigee) / 2);
            }
            return alphaInTime;
        }


        public static (Dictionary<DateTime, double>, Dictionary<DateTime, double>) GetDicApogeeAndPerigee(List<List<string>> SortedTle)
        {
            Dictionary<DateTime, double> AllAPogee = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> AllPerig = new Dictionary<DateTime, double>();
            for (int i = 0; i < SortedTle.Count; i++)
            {
                Tle tle = new Tle("", SortedTle[i][0], SortedTle[i][1]);
                Satellite sat = new Satellite(tle);
                DateTime dayOfYear = ConvertNumberToDate(tle.Epoch);
                double numOfRev = double.Parse(tle.MeanMotion.Substring(0, 11).Replace('.', ','));
                int timeInterval = (int)Math.Round(24 * 120 / numOfRev);
                Dictionary<DateTime, double> radVecOfTime = GetRadiusVecOfTime(sat, dayOfYear, timeInterval);
                LookForApoAndPerDate(sat, radVecOfTime, ref AllAPogee, ref AllPerig);
            }
            return (AllAPogee, AllPerig);
        }

        public static void LookForApoAndPerDate(Satellite sat, Dictionary<DateTime, double> radVecOfTime, 
            ref Dictionary<DateTime, double> Arrapogee, ref Dictionary<DateTime, double> Arrperigee)
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
                    if (!Arrapogee.ContainsKey(timeOfApogee)) Arrapogee.Add(timeOfApogee, radApogee);
                }
                if (item.Value == radPerigee)
                {
                    timeOfPerigee = item.Key;
                    if (!Arrperigee.ContainsKey(timeOfPerigee)) Arrperigee.Add(timeOfPerigee, radPerigee);
                }
            }
        }

        //Радиус вектор точки
        public static Dictionary<DateTime, double> GetMediumApoAndPer(List<List<string>> SortedTle)
        {
            Dictionary<DateTime, double> alphaInTime = new Dictionary<DateTime, double>();
            for (int i = 0; i < SortedTle.Count - 1; i++)
            {
                Tle tle = new Tle("", SortedTle[i][0], SortedTle[i][1]);
                Satellite sat = new Satellite(tle);
                DateTime dayOfYear = ConvertNumberToDate(tle.Epoch);
                double numOfRev = double.Parse(tle.MeanMotion.Substring(0, 11).Replace('.', ','));
                int timeInterval = (int)Math.Round(24 * 120 / numOfRev);
                Dictionary<DateTime, double> radVecOfTime = GetRadiusVecOfTime(sat, dayOfYear, timeInterval);
                double radApogee = radVecOfTime.Values.Max();
                double radPerigee = radVecOfTime.Values.Min();
                double mediumRadApoAndPer = (radApogee + radPerigee) / 2;
                alphaInTime.Add(dayOfYear, mediumRadApoAndPer);
            }
            return alphaInTime;
        }

        //Радиус вектор точки
        public static Dictionary<DateTime, double> GetRadiusFull(List<List<string>> SortedTle)
        {
            Dictionary<DateTime, double> alphaInTime = new Dictionary<DateTime, double>();
            for (int i = 0; i < SortedTle.Count - 1; i++)
            {
                Tle tle1 = new Tle("", SortedTle[i][0], SortedTle[i][1]);
                Satellite sat1 = new Satellite(tle1);
                var lcflfc = sat1.PositionEci(0);
                var lssll = ModuleRadius(lcflfc);
                DateTime dayOfYear = ConvertNumberToDate(tle1.Epoch);
                for (int j = 0; j < 720; j++)
                {
                    Eci eci = sat1.PositionEci(dayOfYear);
                    alphaInTime.Add(dayOfYear, ModuleRadius(eci));
                    dayOfYear = dayOfYear.AddMinutes(2);
                }
            }
            return alphaInTime;
        }

        public static Dictionary<DateTime, double> GetRad(List<List<string>> SortedTle)
        {
            Dictionary<DateTime, double> alphaInTime = new Dictionary<DateTime, double>();
            for (int i = 0; i < SortedTle.Count - 1; i++)
            {
                Tle tle1 = new Tle("", SortedTle[i][0], SortedTle[i][1]);
                Tle tle2 = new Tle("", SortedTle[i + 1][0], SortedTle[i + 1][1]);
                Satellite sat1 = new Satellite(tle1);
                Satellite sat2 = new Satellite(tle2);
                Orbit orb1 = new Orbit(tle1);
                double numOfRev1 = double.Parse(tle1.MeanMotion.Substring(0, 11).Replace('.', ','));
                var vAp1AndVPer1 = GetVelOfApAndPer(tle1, sat1);
                var vAp2AndVPer2 = GetVelOfApAndPer(tle2, sat2);
                double vAp1 = vAp1AndVPer1.Item1;
                double vAp2 = vAp2AndVPer2.Item1;
                double vPer1 = vAp1AndVPer1.Item2;
                double vPer2 = vAp2AndVPer2.Item2;
                double alphaApogee = CalculateAlphaKinetic(vAp1, vAp2, numOfRev1, orb1.Apogee, orb1.Perigee, orb1.Eccentricity);
                double alphaPerigee = CalculateAlphaKinetic(vPer1, vPer2, numOfRev1, orb1.Apogee, orb1.Perigee, orb1.Eccentricity);
                alphaInTime.Add(ConvertNumberToDate(tle1.Epoch), (alphaApogee + alphaPerigee) / 2);
            }
            return alphaInTime;
        }

        public static (double, double) GetVelOfApAndPer(Tle tle, Satellite sat)
        {
            DateTime dayOfYear = ConvertNumberToDate(tle.Epoch);
            double numOfRev = double.Parse(tle.MeanMotion.Substring(0, 11).Replace('.', ','));
            int timeInterval = (int)Math.Round(24 * 120 / numOfRev);
            Dictionary<DateTime, double> radVecOfTime = GetRadiusVecOfTime(sat, dayOfYear, timeInterval);
            var VelosOfApAndPer = LookForApoAndPerVelos(sat, radVecOfTime);
            double vAp = VelosOfApAndPer.Item1;
            double vPer = VelosOfApAndPer.Item2;
            return (vAp, vPer);
        }

        public static (double, double) LookForApoAndPerVelos(Satellite sat, Dictionary<DateTime, double> radVecOfTime)
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
            double vAp = GetVelosit(sat, timeOfApogee);
            double vPer = GetVelosit(sat, timeOfPerigee);
            return (vAp, vPer);
        }

        public static Dictionary<DateTime, double> GetRadiusVecOfTime(Satellite sat, DateTime dayOfYear, int timeInterval)
        {
            Dictionary<DateTime, double> radVecOfTime = new Dictionary<DateTime, double>();
            for (int j = 0; j < timeInterval; j++)
            {
                Eci eci = sat.PositionEci(dayOfYear);
                Geo geo = new Geo(eci, new Julian(dayOfYear));
                double ff = geo.LatitudeDeg;
                double ff1 = geo.LongitudeRad;
                double radVec = ModuleRadius(eci);
                radVecOfTime.Add(dayOfYear, radVec);
                dayOfYear = dayOfYear.AddSeconds(30);
            }
            return radVecOfTime;
        }

        public static double GetVelosit(Satellite sat, DateTime date)
        {
            Eci eci = sat.PositionEci(date);
            return ModuleVelos(eci);
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

        //Формируем словарь из коэф.альфа и соответствующих дат
        public static Dictionary<DateTime, double> FormResultDictionaryEnd(List<List<string>> SortedTle)
        {
            Dictionary<DateTime, double> resultPoten = new Dictionary<DateTime, double>();
            var res1 = GetDicApogeeAndPerigee(SortedTle).Item1;
            var radNededPoint = GetCycloidPoints(res1).Values.ToList();
            var resultForTwoMethod = new Dictionary<DateTime, double>[2];
            for (int i = 0; i < SortedTle.Count - 3; i++)
            {
                Tle tle1 = new Tle("", SortedTle[i][0], SortedTle[i][1]);
                Tle tle2 = new Tle("", SortedTle[i + 1][0], SortedTle[i + 1][1]);
                Orbit orb1 = new Orbit(tle1);
                var resAlpha = CalculateAlphaEnd(tle1, tle2, orb1, radNededPoint[i], radNededPoint[i + 1]);
                resultPoten.Add(ConvertNumberToDate(tle1.Epoch), resAlpha);
            }
            return resultPoten;
        }

        //Находим нужные переменные для вычисления альфа
        private static double CalculateAlphaEnd(Tle tle1, Tle tle2, Orbit orb1, double r1, double r2)
        {
            Satellite sat1 = new Satellite(tle1);
            Eci eci1 = sat1.PositionEci(0);
            double v1 = ModuleVel(eci1); //Считаем модуль скорости
            double numOfRev = double.Parse(tle1.MeanMotion.Substring(0, 11).Replace('.', ','));
            double apogee = orb1.Apogee;
            double perigee = orb1.Perigee;
            double eccent = orb1.Eccentricity;
            double AlphaPot = CalculateAlphaPotentialEnd(r1, r2, v1, numOfRev, apogee, perigee, eccent);
            return  AlphaPot;
        }

        //Подставляем в формулу
        private static double CalculateAlphaKinetic(double v1, double v2, double numOfRev, double apogee,
                                                    double perigee, double eccent)
        {
            int weightSatel = 1900; // масса ступника
            return Math.Abs(weightSatel * (v2 * v2 - v1 * v1) / (2 * numOfRev * v1 * v1
                * CalcLenEllipse(apogee, perigee, eccent)));
        }

        private static double CalculatePotential(double r1, double r2)
        {
            int weightSatel = 1900; // Масса спутника
            const double constGmultiplyM = 398345.740; //Умножили G на массу Земли M (размерность км^3 / с^2)
            return Math.Abs(weightSatel * constGmultiplyM * ((1 / r2) - (1 / r1)));
        }

        //Подставляем в формулу
        private static double CalculateAlphaPotentialEnd(double r1, double r2, double v1, double numOfRev,
                                                      double apogee, double perigee, double eccent)
        {
            int weightSatel = 1900; // Масса спутника
            double constGmultiplyM = 398345.740; //Умножили G на массу Земли M (размерность км^3 / с^2)
            return Math.Abs(weightSatel * constGmultiplyM * 2 * ((1 / r2) - (1 / r1)) / (numOfRev * v1 * v1
                * CalcLenEllipse(apogee, perigee, eccent)));
        }

        //Найдем модуль скорости
        private static double ModuleVel(Eci eci)
        {
            return Math.Sqrt(eci.Velocity.X * eci.Velocity.X + eci.Velocity.Y * eci.Velocity.Y 
                + eci.Velocity.Z * eci.Velocity.Z);
        }

        //Найдем модуль радиус вектора
        //private static double ModuleDistance(Tle tle)
        //{
        //    Satellite sat = new Satellite(tle);
        //    Eci eci = sat.PositionEci(0);
        //    return Math.Sqrt(eci.Position.X * eci.Position.X + eci.Position.Y * eci.Position.Y
        //        + eci.Position.Z * eci.Position.Z);
        //}

        //Перевод количества дней в дату
        public static DateTime ConvertNumberToDate(string number)
        {
            DateTime date1 = new DateTime();
            int year = int.Parse("20" + number.Substring(0, 2));
            int day = int.Parse(number.Substring(2, 3));
            Dictionary<int, int> AllMonth = CreateCalendar(year);
            int checkNum = day;
            for (int i = 1; i < 13 ; i++)
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
        public static List<List<string>> ChooseTle(List<List<string>> AllTle, DateTime date1, DateTime date2)
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

        //Читаем коэффициент B из Tle файла
        public static Dictionary<DateTime, double> ChooseCoeffB(List<List<string>> SortTle)
        {
            Dictionary<DateTime, double> coeffB = new Dictionary<DateTime, double>();
            int massSatt = 1900;
            for (int i = 0; i < SortTle.Count; i++)
            {
                Tle tle = new Tle("", SortTle[i][0], SortTle[i][1]);
                string rawB = SortTle[i][0].Substring(54, 7);
                string ofovfkfvk = rawB.Substring(rawB.Length - 1);
                string odfvkfdvjfdvj = rawB.Substring(0, 5);
                int degree = - 5 - int.Parse(rawB.Substring(rawB.Length - 1));
                double B = massSatt * int.Parse(rawB.Substring(0, 5)) * Math.Pow(10, degree);
                coeffB.Add(ConvertNumberToDate(tle.Epoch), B);
            }
            return coeffB;
        }

        //Усредняем значения в массиве
        public static Dictionary<DateTime, double> MakeSlidingCurve(Dictionary<DateTime, double> coeffB)
        {
            Dictionary<DateTime, double> coeffBAver = new Dictionary<DateTime, double>();
            List<DateTime> KeysCoeffB = coeffB.Keys.ToList();
            List<double> ValuesCoeffB = coeffB.Values.ToList();
            List<double> averages1 = Enumerable.Range(0, ValuesCoeffB.Count - 59).
                              Select(i => ValuesCoeffB.Skip(i).Take(30).Average()).ToList();
            ValuesCoeffB.Reverse();
            List<double> averages2 = Enumerable.Range(0, ValuesCoeffB.Count - 59).
                              Select(i => ValuesCoeffB.Skip(i).Take(30).Average()).ToList();
            averages2.Reverse();
            averages2.RemoveRange(0, coeffB.Count - 118);
            averages1.AddRange(averages2);
            for (int i = 0; i < coeffB.Count; i++)
            {
                coeffBAver.Add(KeysCoeffB[i], averages1[i]);
            }
            return coeffBAver;
        }

        public static Dictionary<DateTime, double> MakeSlidingCurveUp(Dictionary<DateTime, double> coeffB)
        {
            Dictionary<DateTime, double> coeffBAver = new Dictionary<DateTime, double>();
            List<DateTime> KeysCoeffB = coeffB.Keys.ToList();
            List<double> ValuesCoeffB = coeffB.Values.ToList();
            List<double> averages = Enumerable.Range(0, ValuesCoeffB.Count - 49).
                              Select(i => ValuesCoeffB.Skip(i).Take(50).Average()).ToList();
            ValuesCoeffB.RemoveRange(49, ValuesCoeffB.Count - 49);
            averages.AddRange(ValuesCoeffB);
            for (int i = 0; i < coeffB.Count; i++)
            {
                coeffBAver.Add(KeysCoeffB[i], averages[i]);
            }
            return coeffBAver;
        }

        public static Dictionary<DateTime, double> MakeSlidingCurveDown(Dictionary<DateTime, double> coeffB)
        {
            Dictionary<DateTime, double> coeffBAver = new Dictionary<DateTime, double>();
            List<DateTime> KeysCoeffB = coeffB.Keys.ToList();
            List<double> ValuesCoeffB = coeffB.Values.ToList();
            ValuesCoeffB.Reverse();
            List<double> averages = Enumerable.Range(0, ValuesCoeffB.Count - 49).
                              Select(i => ValuesCoeffB.Skip(i).Take(50).Average()).ToList();
            averages.Reverse();
            ValuesCoeffB.RemoveRange(0, ValuesCoeffB.Count - 49);
            averages.AddRange(ValuesCoeffB);
            for (int i = 0; i < coeffB.Count; i++)
            {
                coeffBAver.Add(KeysCoeffB[i], averages[i]);
            }
            return coeffBAver;
        }

        //Усредняем значения в массиве
        public static Dictionary<DateTime, double> MakeSlidingCurveBetter(Dictionary<DateTime, double> coeffB)
        {
            Dictionary<DateTime, double> coeffBAver = coeffB;
            int amount = 5;
            bool flag = true;
            for (int i = 0; i < amount; i++)
            {
                if(flag)
                {
                    coeffBAver = MakeSlidingCurveUp(coeffBAver);
                    flag = false;
                }
                else
                {
                    coeffBAver = MakeSlidingCurveDown(coeffBAver);
                    flag = true;
                }
            }
            return coeffBAver;
        }

        public static List<double> ReadTleFromTxtFile(string path)
        {
            List<string> AllCoeff = new List<string>();
            List<double> AllCoeffdouble = new List<double>();
            StreamReader sr = new StreamReader(path, System.Text.Encoding.Default);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                AllCoeff.Add(line);
            }
            AllCoeffdouble = AllCoeff.ConvertAll(double.Parse);
            return AllCoeffdouble;
        }

        //Вычисляем длину орбиты с заданными апогеем, перигеем и эксцентриситетом
        private static double CalcLenEllipse(double apogee, double perigee, double eccent)
        {
            int radEarth = 6371;
            double a = (apogee + perigee + 2 * radEarth) / 2;
            double b = a * Math.Sqrt(1 - eccent * eccent);
            return (4 * (Math.PI * a * b + (a - b) * (a - b)) / (a + b));
        }
    }
}
