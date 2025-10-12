using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bai011
{
    internal class Program
    {
        // Hàm kiểm tra số nguyên tố
        static bool LaSoNguyenTo(int n)
        {
            if (n < 2) return false;
            for (int i = 2; i <= Math.Sqrt(n); i++)
                if (n % i == 0) return false;
            return true;
        }

        // Hàm kiểm tra số chính phương
        static bool LaSoChinhPhuong(int n)
        {
            int can = (int)Math.Sqrt(n);
            return can * can == n;
        }
        public static void Main(string[] args)
        {
            Console.Write("Nhap so luong phan tu : ");
            int n = int.Parse(Console.ReadLine());
            int[] a = new int[n];
            Random r = new Random();
            int tongLe = 0, slNguyenTo = 0;
            int? scp = 101;
            for (int i = 0; i < n; i++)
            {
                a[i] = r.Next(100);
            }

            Console.WriteLine("Mang vua duoc tao : ");
            for(int i=0;i<n;i++) Console.Write(a[i] + " ");


            for (int i = 0; i < n; i++)
                if (a[i] % 2 == 1) tongLe = tongLe + a[i];
            Console.WriteLine("\nTong cac so le trong mang : " + tongLe);

            for (int i = 0; i < n; i++)
            {
                if (LaSoNguyenTo(a[i])) slNguyenTo++;
            }
            Console.WriteLine("So luong so nguyen to trong mang : " + slNguyenTo);

            for (int i = 0; i < n; i++)
            {
                if (LaSoChinhPhuong(a[i]))
                {
                    if (scp > a[i]) scp = a[i];
                }
            }
            if (scp == 101)
            {
                Console.WriteLine("-1");
            }
            else
            {
                Console.WriteLine("So chinh phuong nho nhat trong mang : " + scp);
            }
        }
    }
}
