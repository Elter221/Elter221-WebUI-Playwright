using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nUnitWebTests
{
    public class SearchPage(IPage page)
    {
        private readonly string _headerSearch = "//div[@class='header-search']";
        private const string _searchField = "//div//input[@class='form-control']";
        private const string _submitButton = "//div//button[contains(text(), 'Search')]";

        public async Task<SearchPage> SearchProgramAsync(string search)
        {
            await page.Locator(_headerSearch).ClickAsync();
            await page.Locator(_searchField).FillAsync(search);
            await page.Locator(_submitButton).ClickAsync();
            return this;
        }
    }
}
