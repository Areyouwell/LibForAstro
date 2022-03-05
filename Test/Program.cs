using System;
using System.Collections.Generic;
using System.IO;
using Zeptomoby.OrbitTools;


namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime datee = new DateTime(2017, 01, 15);
            DateTime datee2 = datee.AddYears(1);
            string path = @"D:\Lesson\programming\Си #\astro\tle 2009-2019.txt";
            var AllTle = ReadTleFromFile(path);
            var SortTle = ChoiseTle(AllTle, datee, datee2);
            var res = FormResultDictionary(SortTle);
            int a = 1;
            Console.WriteLine("lol");
        }

        //Формируем словарь из коэф. альфа и соответствующих дат
        public static Dictionary<DateTime, double> [] FormResultDictionary(List<List<string>> SortedTle)
        {
            Dictionary<DateTime, double> resultKinet = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> resultPoten = new Dictionary<DateTime, double>();
            var resultForTwoMethod = new Dictionary<DateTime, double>[2];
            for (int i = 0; i < SortedTle.Count - 1; i++)
            {
                Tle tle1 = new Tle("", SortedTle[i][0], SortedTle[i][1]);
                Tle tle2 = new Tle("", SortedTle[i + 1][0], SortedTle[i + 1][1]);
                Orbit orb1 = new Orbit(tle1);
                var resAlpha = CalculateAlpha(tle1, tle2, orb1);
                resultKinet.Add(ConvertNumberToDate(tle1.Epoch), resAlpha.Item1);
                resultPoten.Add(ConvertNumberToDate(tle1.Epoch), resAlpha.Item2);
            }
            resultForTwoMethod[0] = resultKinet;
            resultForTwoMethod[1] = resultPoten;
            return resultForTwoMethod;
        }

        //Находим нужные переменные для вычисления альфа
        private static (double, double) CalculateAlpha(Tle tle1, Tle tle2, Orbit orb1)
        {
            double v1 = ModuleVel(tle1);
            double v2 = ModuleVel(tle2);
            double r1 = ModuleDistance(tle1);
            double r2 = ModuleDistance(tle2);
            double numOfRev = double.Parse(tle1.MeanMotion.Substring(0, 11).Replace('.', ','));
            double apogee = orb1.Apogee;
            double perigee = orb1.Perigee;
            double eccent = orb1.Eccentricity;
            double AlphaKin = CalculateAlphaKinetic(v1, v2, numOfRev, apogee, perigee, eccent);
            double AlphaPot = CalculateAlphaPotential(r1, r2, v1, numOfRev, apogee, perigee, eccent);
            return (AlphaKin, AlphaPot);
        }

        //Подставляем в формулу
        private static double CalculateAlphaKinetic(double v1, double v2, double numOfRev, double apogee,
                                                    double perigee, double eccent)
        {
            int weightSatel = 1900; // масса ступника
            return Math.Abs(weightSatel * (v2 * v2 - v1 * v1) / (2 * numOfRev * v1 * v1
                * CalcLenEllipse(apogee, perigee, eccent)));
        }

        //Подставляем в формулу
        private static double CalculateAlphaPotential(double r1, double r2, double v1, double numOfRev, 
                                                      double apogee, double perigee, double eccent)
        {
            int weightSatel = 1900; // Масса спутника
            double constGmultiplyM = 398345.740; //Умножили G на массу Земли M (размерность км^3 / с^2)
            return Math.Abs(weightSatel * constGmultiplyM * 2 * ((1 / r2) - (1 / r1)) / ( numOfRev * v1 * v1
                * CalcLenEllipse(apogee, perigee, eccent)));
        }

        //Найдем модуль скорости
        private static double ModuleVel(Tle tle)
        {
            Satellite sat = new Satellite(tle);
            Eci eci = sat.PositionEci(0);
            return Math.Sqrt(eci.Velocity.X * eci.Velocity.X + eci.Velocity.Y * eci.Velocity.Y 
                + eci.Velocity.Z * eci.Velocity.Z);
        }

        //Найдем модуль радиус вектора
        private static double ModuleDistance(Tle tle)
        {
            Satellite sat = new Satellite(tle);
            Eci eci = sat.PositionEci(0);
            return Math.Sqrt(eci.Position.X * eci.Position.X + eci.Position.Y * eci.Position.Y
                + eci.Position.Z * eci.Position.Z);
        }

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
    }
}
