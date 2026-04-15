using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Control
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var rnd = new Random();

            while (true)
            {
                Console.WriteLine("请输入10个数，按回车确认每个数（程序会额外生成一个0到1000之间的随机整数并一并参与比较）：");
                var nums = new List<double>();
                while (nums.Count < 10)
                {
                    Console.Write($"第{nums.Count + 1}个数: ");
                    string input = Console.ReadLine();
                    if (double.TryParse(input, out double v))
                    {
                        nums.Add(v);
                    }
                    else
                    {
                        Console.WriteLine("输入无效，请输入数字。\n");
                    }
                }

                // 生成一个 0 到 1000（含）之间的随机整数并加入比较列表
                int randomInt = rnd.Next(0, 1001);
                nums.Add(randomInt);

                double max = nums.Max();
                double min = nums.Min();
                var sorted = nums.OrderBy(x => x).ToList();

                Console.WriteLine();
                Console.WriteLine($"生成的随机整数: {randomInt}");
                Console.WriteLine($"最大值: {max}");
                Console.WriteLine($"最小值: {min}");
                Console.WriteLine("从小到大排序后的所有值（包含随机整数）：");
                Console.WriteLine(string.Join(", ", sorted));

                Console.WriteLine();
                Console.Write("是否继续？继续按Y，不继续按N退出：");
                string ans = Console.ReadLine();
                if (!(ans?.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) == true))
                {
                    Console.WriteLine("程序退出，按任意键结束...");
                    Console.ReadKey();
                    break;
                }
                Console.WriteLine();
            }
        }
    }
}
