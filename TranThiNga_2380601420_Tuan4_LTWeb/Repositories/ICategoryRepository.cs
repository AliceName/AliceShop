using TranThiNga_2380601420_Tuan4_LTWeb.Models;

namespace TranThiNga_2380601420_Tuan4_LTWeb.Repositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category> GetByIdAsync(int id);
        Task AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(int id);
    }
}
