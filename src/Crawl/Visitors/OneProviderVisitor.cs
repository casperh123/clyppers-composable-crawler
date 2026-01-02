using AngleSharp.Html.Dom;
using AngleSharp.Dom;
using Crawl.Core;
using Crawl.Models;
using System.Linq;

namespace Crawl.Visitors;

public class ServerInfo
{
    public string Location { get; set; } = string.Empty;
    public string CpuModel { get; set; } = string.Empty;
    public string CpuFrequency { get; set; } = string.Empty;
    public string CpuCores { get; set; } = string.Empty;
    public string Ram { get; set; } = string.Empty;
    public string RamType { get; set; } = string.Empty;
    public string Storage { get; set; } = string.Empty;
    public string Bandwidth { get; set; } = string.Empty;
    public string PriceEur { get; set; } = string.Empty;
    public string PriceUsd { get; set; } = string.Empty;
    public string Stock { get; set; } = string.Empty;
    
    public override string ToString()
    {
        return $"Location: {Location}, CPU: {CpuModel} {CpuFrequency} ({CpuCores}), RAM: {Ram} {RamType}, Storage: {Storage}, Bandwidth: {Bandwidth}, Price: €{PriceEur}/month (${PriceUsd}/month), Stock: {Stock}";
    }
}

public class OneProviderVisitor : ICrawlVisitor
{
    public ICollection<ServerInfo> servers = [];
    
    public Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken cancellationToken = default)
    {
        if (document == null)
            return Task.CompletedTask;

        // Find all server rows
        var serverRows = document.QuerySelectorAll(".results-tr.dedicated-server");
        
        foreach (var row in serverRows)
        {
            var serverInfo = new ServerInfo();
            
            // Extract Location
            var locationElement = row.QuerySelector(".field-location-name");
            if (locationElement != null)
                serverInfo.Location = locationElement.TextContent.Trim();
            
            // Extract CPU information
            var cpuName = row.QuerySelector(".field-cpu-name");
            if (cpuName != null)
                serverInfo.CpuModel = cpuName.TextContent.Trim();
            
            var cpuFreq = row.QuerySelector(".field-cpu-freq");
            if (cpuFreq != null)
                serverInfo.CpuFrequency = cpuFreq.TextContent.Trim();
            
            var cpuCore = row.QuerySelector(".field-cpu-core");
            if (cpuCore != null)
                serverInfo.CpuCores = cpuCore.TextContent.Trim();
            
            // Extract RAM
            var ramAmount = row.QuerySelector(".field--base-ram-amount .digits");
            var ramUnit = row.QuerySelector(".field--base-ram-amount .unit");
            if (ramAmount != null && ramUnit != null)
                serverInfo.Ram = $"{ramAmount.TextContent.Trim()}{ramUnit.TextContent.Trim()}";
            
            var ramType = row.QuerySelector(".field--ram-type");
            if (ramType != null)
                serverInfo.RamType = ramType.TextContent.Trim();
            
            // Extract Storage (first option)
            var storageElements = row.QuerySelectorAll(".field--drives > div");
            if (storageElements.Any())
            {
                var firstStorage = storageElements[0];
                var storageUnit = firstStorage.QuerySelector(".unit:first-child")?.TextContent.Trim() ?? "";
                var storageCapacityDigits = firstStorage.QuerySelector(".capacity .digits")?.TextContent.Trim() ?? "";
                var storageCapacityUnit = firstStorage.QuerySelector(".capacity .unit")?.TextContent.Trim() ?? "";
                var storageType = firstStorage.QuerySelectorAll(".unit");
                var type = storageType.Skip(1).Select(e => e.TextContent.Trim()).Where(s => !string.IsNullOrEmpty(s));
                
                serverInfo.Storage = $"{storageUnit} {storageCapacityDigits}{storageCapacityUnit} {string.Join(" ", type)}".Trim();
            }
            
            // Extract Bandwidth
            var bwSpeed = row.QuerySelector(".field--bw-speed .digits");
            var bwSpeedUnit = row.QuerySelector(".field--bw-speed .unit");
            var bwLimit = row.QuerySelector(".field--bw-limit .digits");
            var bwLimitUnit = row.QuerySelector(".field--bw-limit .unit");
            
            if (bwSpeed != null && bwSpeedUnit != null)
            {
                serverInfo.Bandwidth = $"{bwSpeed.TextContent.Trim()}{bwSpeedUnit.TextContent.Trim()}";
                if (bwLimit != null && bwLimitUnit != null)
                    serverInfo.Bandwidth += $" ({bwLimit.TextContent.Trim()}{bwLimitUnit.TextContent.Trim()})";
            }
            
            // Extract Prices
            var priceEur = row.QuerySelector(".currency-code-eur .price-amount");
            var priceCentEur = row.QuerySelector(".currency-code-eur .price-cent");
            if (priceEur != null && priceCentEur != null)
                serverInfo.PriceEur = $"{priceEur.TextContent.Trim()}.{priceCentEur.TextContent.Trim()}";
            
            var priceUsd = row.QuerySelector(".currency-code-usd .price-amount");
            var priceCentUsd = row.QuerySelector(".currency-code-usd .price-cent");
            if (priceUsd != null && priceCentUsd != null)
                serverInfo.PriceUsd = $"{priceUsd.TextContent.Trim()}.{priceCentUsd.TextContent.Trim()}";
            
            // Extract Stock status
            var stockIcon = row.QuerySelector(".res-stock .fa-check-circle");
            serverInfo.Stock = stockIcon != null ? "In Stock" : "Out of Stock";
            
            servers.Add(serverInfo);
        }
        
        return Task.CompletedTask;
    }
}