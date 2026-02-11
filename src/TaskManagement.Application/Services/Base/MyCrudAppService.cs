using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Services.Base
{
    public abstract class MyCrudAppService<TEntity, TEntityDto, TKey, TGetListInput, TCreateInput, TUpdateInput>
        : ApplicationService
        where TEntity : class, IEntity<TKey>
        where TEntityDto : IEntityDto<TKey>
        where TGetListInput : PagedAndSortedResultRequestDto
    {
        protected IRepository<TEntity, TKey> Repository { get; }

        protected string GetPolicyName { get; set; }
        protected string GetListPolicyName { get; set; }
        protected string CreatePolicyName { get; set; }
        protected string UpdatePolicyName { get; set; }
        protected string DeletePolicyName { get; set; }

        protected MyCrudAppService(IRepository<TEntity, TKey> repository)
        {
            Repository = repository;
        }

        public virtual async Task<TEntityDto> GetAsync(TKey id)
        {
            await CheckPolicyAsync(GetPolicyName);

            var entity = await Repository.GetAsync(id);
            return ObjectMapper.Map<TEntity, TEntityDto>(entity);
        }

        public virtual async Task<PagedResultDto<TEntityDto>> GetListAsync(TGetListInput input)
        {
            await CheckPolicyAsync(GetListPolicyName);
            var query = await Repository.GetQueryableAsync();

            query = ApplyFiltering(query, input);

            var totalCount = await AsyncExecuter.CountAsync(query);
            query = ApplySorting(query, input);

            query = ApplyPaging(query, input);

            var entities = await AsyncExecuter.ToListAsync(query);

            return new PagedResultDto<TEntityDto>(
                totalCount,
                ObjectMapper.Map<List<TEntity>, List<TEntityDto>>(entities)
            );
        }
        public virtual async Task<TEntityDto> CreateAsync(TCreateInput input)
        {
            await CheckPolicyAsync(CreatePolicyName);

            var entity = ObjectMapper.Map<TCreateInput, TEntity>(input);

            await Repository.InsertAsync(entity, autoSave: true);

            return ObjectMapper.Map<TEntity, TEntityDto>(entity);
        }

        public virtual async Task<TEntityDto> UpdateAsync(TKey id, TUpdateInput input)
        {
            await CheckPolicyAsync(UpdatePolicyName);

            var entity = await Repository.GetAsync(id);
            ObjectMapper.Map(input, entity);

            await Repository.UpdateAsync(entity, autoSave: true);

            return ObjectMapper.Map<TEntity, TEntityDto>(entity);
        }
        public virtual async Task DeleteAsync(TKey id)
        {
            await CheckPolicyAsync(DeletePolicyName);
            await Repository.DeleteAsync(id, autoSave: true);
        }

        protected virtual async Task CheckPolicyAsync(string policyName)
        {
            if (string.IsNullOrEmpty(policyName)) return;
            await AuthorizationService.CheckAsync(policyName);
        }

        protected virtual IQueryable<TEntity> ApplyFiltering(IQueryable<TEntity> query, TGetListInput input)
        {
            return query;
        }

        protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, TGetListInput input)
        {
            if (!input.Sorting.IsNullOrWhiteSpace())
            {
                return query.OrderBy(input.Sorting);
            }
            return query.OrderBy(e => e.Id);
        }

        protected virtual IQueryable<TEntity> ApplyPaging(IQueryable<TEntity> query, TGetListInput input)
        {
            return query.PageBy(input);
        }
    }

    public abstract class MyCrudAppService<TEntity, TEntityDto, TKey, TGetListInput, TCreateUpdateInput>
        : MyCrudAppService<TEntity, TEntityDto, TKey, TGetListInput, TCreateUpdateInput, TCreateUpdateInput>
        where TEntity : class, IEntity<TKey>
        where TEntityDto : IEntityDto<TKey>
        where TGetListInput : PagedAndSortedResultRequestDto
    {
        protected MyCrudAppService(IRepository<TEntity, TKey> repository)
            : base(repository)
        {
        }
    }
}
