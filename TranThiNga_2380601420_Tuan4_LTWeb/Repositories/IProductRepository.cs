using TranThiNga_2380601420_Tuan4_LTWeb.Models;

namespace TranThiNga_2380601420_Tuan4_LTWeb.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);

    }
}
