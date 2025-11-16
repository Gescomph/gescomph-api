using Business.Interfaces.Implements.AdministrationSystem;
using Business.Interfaces.Notifications;
using Business.Repository;
using Data.Interfaz.IDataImplement.AdministrationSystem;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.DTOs.Implements.Utilities;
using Entity.Enum;
using MapsterMapper;
using Utilities.Exceptions;

namespace Business.Services.AdministrationSystem
{
    public class NotificationService
        : BusinessGeneric<NotificationDto, NotificationCreateDto, NotificationUpdateDto, Notification>,
          INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly INotificationRealtimeService _realtime;

        public NotificationService(
            INotificationRepository repository,
            INotificationRealtimeService realtime,
            IMapper mapper)
            : base(repository, mapper)
        {
            _repository = repository;
            _realtime = realtime;
        }

        public override async Task<NotificationDto> CreateAsync(NotificationCreateDto dto)
        {
            var notification = _mapper.Map<Notification>(dto);
            notification.Status = NotificationStatus.Unread;

            var created = await _repository.AddAsync(notification);
            var dtoCreated = _mapper.Map<NotificationDto>(created);

            await _realtime.PushAsync(dtoCreated);

            return dtoCreated;
        }

        public override async Task<NotificationDto> UpdateAsync(NotificationUpdateDto dto)
        {
            var entity = await _repository.GetByIdAsync(dto.Id)
                ?? throw new BusinessException("La notificación no existe.");

            _mapper.Map(dto, entity);

            var updated = await _repository.UpdateAsync(entity);
            return _mapper.Map<NotificationDto>(updated);
        }

        public async Task<IReadOnlyList<NotificationDto>> GetFeedAsync(int userId, NotificationStatus? status = null, int take = 20)
        {
            var notifications = await _repository.GetByUserAsync(userId, status, take);
            return notifications.Select(n => _mapper.Map<NotificationDto>(n)).ToList();
        }

        public async Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(int userId)
        {
            var notifications = await _repository.GetUnreadByUserAsync(userId);
            return notifications.Select(n => _mapper.Map<NotificationDto>(n)).ToList();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _repository.GetByIdAsync(notificationId);
            if (notification is null || notification.RecipientUserId != userId || notification.IsDeleted)
            {
                return false;
            }

            if (notification.Status == NotificationStatus.Read)
            {
                return true;
            }

            notification.Status = NotificationStatus.Read;
            notification.ReadAt = DateTime.UtcNow;
            await _repository.UpdateAsync(notification);
            return true;
        }

        public Task<int> MarkAllAsReadAsync(int userId)
        {
            return _repository.MarkAllAsReadAsync(userId);
        }
    }
}