using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

// Классы сотрудников остаются без изменений
public abstract class Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime EmploymentDate { get; set; }
    public decimal Rate { get; set; }
    public string EmployeeType { get; protected set; }

    public Employee(string name, DateTime employmentDate, decimal rate)
    {
        Name = name;
        EmploymentDate = employmentDate;
        Rate = rate;
    }

    public abstract void GetInfo();
    public abstract decimal GetPrice();
}

public class KitchenWorker : Employee
{
    public int HoursWorked { get; set; }

    public KitchenWorker(string name, DateTime employmentDate, decimal rate, int hoursWorked) 
        : base(name, employmentDate, rate)
    {
        HoursWorked = hoursWorked;
        EmployeeType = "KitchenWorker";
    }

    public override void GetInfo()
    {
        Console.WriteLine($"Работник кухни: {Name}, Дата трудоустройства: {EmploymentDate.ToShortDateString()}, Ставка: {Rate}, Отработано часов: {HoursWorked}");
    }

    public override decimal GetPrice()
    {
        return Rate * HoursWorked;
    }
}

public class Waiter : Employee
{
    public int HoursWorked { get; set; }
    public decimal Tips { get; set; }

    public Waiter(string name, DateTime employmentDate, decimal rate, int hoursWorked, decimal tips) 
        : base(name, employmentDate, rate)
    {
        HoursWorked = hoursWorked;
        Tips = tips;
        EmployeeType = "Waiter";
    }

    public override void GetInfo()
    {
        Console.WriteLine($"Официант: {Name}, Дата трудоустройства: {EmploymentDate.ToShortDateString()}, Ставка: {Rate}, Отработано часов: {HoursWorked}, Чаевые: {Tips}");
    }

    public override decimal GetPrice()
    {
        return Rate * HoursWorked + Tips;
    }
}

public class Manager : Employee
{
    public decimal Bonus { get; set; }

    public Manager(string name, DateTime employmentDate, decimal rate, decimal bonus) 
        : base(name, employmentDate, rate)
    {
        Bonus = bonus;
        EmployeeType = "Manager";
    }

    public override void GetInfo()
    {
        Console.WriteLine($"Менеджер: {Name}, Дата трудоустройства: {EmploymentDate.ToShortDateString()}, Ставка: {Rate}, Премия: {Bonus}");
    }

    public override decimal GetPrice()
    {
        int yearsWorked = DateTime.Now.Year - EmploymentDate.Year;
        return Rate + Bonus * yearsWorked;
    }
}

public class JuniorManager : Manager
{
    public JuniorManager(string name, DateTime employmentDate, decimal rate, decimal bonus) 
        : base(name, employmentDate, rate, bonus)
    {
        EmployeeType = "JuniorManager";
    }

    public override decimal GetPrice()
    {
        int yearsWorked = DateTime.Now.Year - EmploymentDate.Year;
        decimal baseSalary = Rate;

        if (DateTime.Now.Month == 6 || DateTime.Now.Month == 12)
        {
            baseSalary += Bonus * yearsWorked;
        }
        return baseSalary;
    }

    public override void GetInfo()
    {
        Console.WriteLine($"Младший менеджер: {Name}, Дата трудоустройства: {EmploymentDate.ToShortDateString()}, Ставка: {Rate}, Премия: {Bonus}");
    }
}

public class RestaurantContext : DbContext
{
    public DbSet<Employee> Employees { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(
                @"Server=localhost\SQLEXPRESS;Database=RestaurantEF;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>()
            .HasDiscriminator<string>("EmployeeType")
            .HasValue<KitchenWorker>("KitchenWorker")
            .HasValue<Waiter>("Waiter")
            .HasValue<Manager>("Manager")
            .HasValue<JuniorManager>("JuniorManager");

        
        modelBuilder.Entity<KitchenWorker>().Property(k => k.HoursWorked).HasColumnName("HoursWorked");
        modelBuilder.Entity<Waiter>().Property(w => w.HoursWorked).HasColumnName("HoursWorked");
        modelBuilder.Entity<Waiter>().Property(w => w.Tips).HasColumnName("Tips");
        modelBuilder.Entity<Manager>().Property(m => m.Bonus).HasColumnName("Bonus");
        modelBuilder.Entity<JuniorManager>().Property(j => j.Bonus).HasColumnName("Bonus");
    }
}

// Класс для работы с БД через Entity
public class RestaurantDatabaseEF
{
    private readonly RestaurantContext _context;

    public RestaurantDatabaseEF()
    {
        _context = new RestaurantContext();
        _context.Database.EnsureCreated(); 
    }

    public void SaveEmployee(Employee employee)
    {
        _context.Employees.Add(employee);
        _context.SaveChanges();
    }

    public List<Employee> LoadEmployees()
    {
        return _context.Employees.ToList();
    }
}

class Program
{
    static void Main()
    {
        var database = new RestaurantDatabaseEF();

        // Создаем тестовых сотрудников
        var employees = new List<Employee>
        {
            new KitchenWorker("Повар1", new DateTime(2023, 1, 15), 100, 160),
            new KitchenWorker("Повар2", new DateTime(2023, 2, 20), 110, 170),
            new Waiter("Официант1", new DateTime(2023, 3, 10), 80, 150, 5000),
            new Waiter("Официант2", new DateTime(2022, 4, 5), 90, 160, 6000),
            new Manager("Менеджер", new DateTime(2020, 5, 2), 20000, 10000),
            new JuniorManager("Младший менеджер", new DateTime(2021, 6, 8), 18000, 8000)
        };

        // Сохраняем сотрудников в базу данных
        foreach (var employee in employees)
        {
            database.SaveEmployee(employee);
        }

        // Загружаем сотрудников из базы данных
        var loadedEmployees = database.LoadEmployees();

        Console.WriteLine("Информация о сотрудниках:");
        Console.WriteLine("-------------------------");
        
        foreach (var employee in loadedEmployees)
        {
            employee.GetInfo();
            Console.WriteLine($"Зарплата: {employee.GetPrice():C}");
            Console.WriteLine();
        }
    }
}