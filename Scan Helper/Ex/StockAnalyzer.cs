using System;
using System.Collections.Generic;
using System.Linq;

namespace ScanHelper
{
    public static class StockAnalyzer
    {
        public static List<MixedZoneResult> FindMixedZoneItems(List<StockRow> rows)
        {
            // группируем по штрихкоду (обычно он уникальный идентификатор товара/размера)
            var groups = rows
                .GroupBy(r => (r.Barcode ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToList();

            var results = new List<MixedZoneResult>();

            foreach (var g in groups)
            {
                var totalQty = g.Sum(x => x.Qty);

                // собираем все места хранения
                var allLocations = g
                    .SelectMany(x => x.Locations ?? new List<string>())
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (allLocations.Count <= 1) continue;

                // определяем зоны по каждому месту
                var zones = allLocations
                    .Select(GetZoneKey)
                    .Where(z => !string.IsNullOrWhiteSpace(z))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // если зона одна — не выводим
                if (zones.Count <= 1) continue;

                // имя товара берем первое непустое
                var name = g.Select(x => x.ProductName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "";

                results.Add(new MixedZoneResult
                {
                    ProductName = name,
                    Barcode = g.Key,
                    TotalQty = totalQty,
                    Zones = string.Join(", ", zones),
                    Locations = string.Join(", ", allLocations)
                });
            }

            // чтобы было красиво — сортировка по названию/штрихкоду
            return results
                .OrderBy(r => r.ProductName)
                .ThenBy(r => r.Barcode)
                .ToList();
        }

        /// <summary>
        /// Логика зон:
        /// - Если формат "05-02-01" и префикс ДО '-' = цифры -> "УЛИЦА 05"
        /// - Если префикс ДО '-' НЕ цифры (Ш3-..., C1-..., MC2-...) -> зона = ТОЛЬКО этот префикс (Ш3 / C1 / MC2)
        /// - Если нет '-', то:
        ///    - МС/MC + цифры -> МС2
        ///    - С + цифры -> С1
        ///    - иначе возвращаем строку как есть (КОМОД, ВІКНО и т.д.)
        /// </summary>
        private static string GetZoneKey(string location)
        {
            if (string.IsNullOrWhiteSpace(location)) return "";

            var s = location.Trim().ToUpperInvariant();

            // Если есть дефис - берём префикс ДО дефиса
            var dash = s.IndexOf('-');
            if (dash > 0)
            {
                var prefix = s.Substring(0, dash).Trim();

                // улицы: 05-02-01 / 08-02-00 => "УЛИЦА 05"
                if (prefix.All(char.IsDigit))
                    return $"УЛИЦА {prefix}";

                // стеллажи/ряды вида Ш3-.. / C1-.. / MC2-.. => зона = только prefix (Ш3 / C1 / MC2)
                return prefix;
            }

            // если нет '-', то ловим варианты МС/MC
            if (s.StartsWith("МС") || s.StartsWith("MC"))
            {
                var tail = new string(s.Skip(2).TakeWhile(char.IsDigit).ToArray());
                return string.IsNullOrWhiteSpace(tail) ? "МС" : $"МС{tail}";
            }

            // вариант С1, С2 (кириллица "С")
            if (s.StartsWith("С"))
            {
                var tail = new string(s.Skip(1).TakeWhile(char.IsDigit).ToArray());
                return string.IsNullOrWhiteSpace(tail) ? "С" : $"С{tail}";
            }

            // всё остальное: КОМОД, ВІКНО, ПОЛКА, и т.д. — считаем отдельной зоной
            return s;
        }
    }
}

