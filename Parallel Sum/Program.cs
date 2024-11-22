using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Parallel_Sum
{
    static void Main(string[] args)
    {
        // Размер списка
        long[] sizes = { /*10_000_000,*/ 100_000_000 }; // Примеры размеров списка (уменьшено для тестирования)
        foreach (var size in sizes)
        {
            // Генерация списка случайных чисел
            var numbers = GenerateRandomNumbers(size);

            // Последовательное суммирование
            MeasureExecutionTime("Sequential Sum", () => SequentialSum(numbers));

            // Параллельное суммирование с использованием Thread
            for (int threadCount = 2; threadCount <= Environment.ProcessorCount; threadCount++)
            {
                MeasureExecutionTime($"Parallel Sum with Threads (Threads: {threadCount})", () => ParallelSumWithThreads(numbers, threadCount));
            }

            // Параллельное суммирование с использованием Task
            for (int taskCount = 2; taskCount <= Environment.ProcessorCount; taskCount++)
            {
                MeasureExecutionTime($"Parallel Sum with Tasks (Tasks: {taskCount})", () => ParallelSumWithTasks(numbers, taskCount));
            }
        }
    }

    /// <summary>
    /// Генерирует случ числа по мере необходимости, а не хранит их все в памяти.
    /// </summary>
    /// <param name="size">размер массива</param>
    /// <returns>IEnumerable<long></returns>
    static IEnumerable<long> GenerateRandomNumbers(long size)
    {
        Random random = new Random();
        for (long i = 0; i < size; i++)
        {
            yield return random.Next(-100, 100); // Генерируем случайные числа от -100 до 100
        }
    }

    /// <summary>
    /// Последовательное сложение
    /// </summary>
    /// <param name="numbers">число</param>
    /// <returns>сумму</returns>
    static long SequentialSum(IEnumerable<long> numbers)
    {
        long sum = 0;
        foreach (var number in numbers)
        {
            sum += number;
        }
        return sum;
    }

    /// <summary>
    /// Параллельное суммирование с использованием Thread
    /// </summary>
    /// <param name="numbers"></param>
    /// <param name="threadCount"></param>
    /// <returns></returns>
    static long ParallelSumWithThreads(IEnumerable<long> numbers, int threadCount)
    {
        long sum = 0;
        long length = numbers.Count();
        Thread[] threads = new Thread[threadCount];
        long[] partialSums = new long[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            int start = (int)(i * length / threadCount);
            int end = (int)((i + 1) * length / threadCount);

            // Убедимся, что последний поток обрабатывает все оставшиеся элементы
            if (i == threadCount - 1)
            {
                end = (int)length;
            }

            int threadIndex = i; // Локальная переменная для замыкания
            threads[i] = new Thread(() =>
            {
            long localSum = 0;
            foreach (var number in numbers.Skip(start).Take(end - start))
            {
                localSum += number;
            }
                partialSums[threadIndex] = localSum; // Используем локальную переменную
            });
            threads[i].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        return partialSums.Sum();
    }

    /// <summary>
    /// Параллельное суммирование с использованием Task
    /// </summary>
    /// <param name="numbers"></param>
    /// <param name="taskCount"></param>
    /// <returns></returns>
    static long ParallelSumWithTasks(IEnumerable<long> numbers, int taskCount)
    {
        long length = numbers.Count();
        Task<long>[] tasks = new Task<long>[taskCount];

        for (int i = 0; i < taskCount; i++)
        {
            int start = (int)(i * length / taskCount);
            int end = (int)((i + 1) * length / taskCount);

            // Убедимся, что последний таск обрабатывает все оставшиеся элементы
            if (i == taskCount - 1)
            {
                end = (int)length;
            }

            int taskIndex = i; // Локальная переменная для замыкания
            tasks[i] = Task.Run(() =>
            {
                long localSum = 0;
                foreach (var number in numbers.Skip(start).Take(end - start))
                {
                    localSum += number;
                }
                return localSum;
            });
        }

        Task.WaitAll(tasks);
        return tasks.Sum(t => t.Result);
    }

    /// <summary>
    /// Подсчёт времени на данное вычисление суммы
    /// </summary>
    /// <param name="description">способ суммирования</param>
    /// <param name="action">lamba функция для Stopwatch</param>
    static void MeasureExecutionTime(string description, Func<long> action)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        long result = action();
        stopwatch.Stop();
        Console.WriteLine($"{description}: {result}, Time: {stopwatch.ElapsedMilliseconds} ms");
    }
}