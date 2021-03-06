import * as signalR from '@aspnet/signalr';

angular.module('platformWebApp')
.config(
  ['$stateProvider', function ($stateProvider) {
      $stateProvider
          .state('workspace.pushNotificationsHistory', {
              url: '/events',
              templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
              controller: ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
                  var blade = {
                      id: 'events',
                      title: 'platform.blades.history.title',
                      breadcrumbs: [],
                      subtitle: 'platform.blades.history.subtitle',
                      controller: 'platformWebApp.pushNotificationsHistoryController',
                      template: '$(Platform)/Scripts/app/pushNotifications/blade/history.tpl.html',
                      isClosingDisabled: true
                  };
                  bladeNavigationService.showBlade(blade);
              }
              ]
          });
  }])
.factory('platformWebApp.pushNotificationTemplateResolver', ['platformWebApp.bladeNavigationService', '$state', function (bladeNavigationService, $state) {
    var notificationTemplates = [];

    function register(template) {
        notificationTemplates.push(template);
        notificationTemplates.sort(function (a, b) { return a.priority - b.priority; });
    };
    function resolve(notification, place) {
        return _.find(notificationTemplates, function (x) { return x.satisfy(notification, place); });
    };
    var retVal = {
        register: register,
        resolve: resolve,
    };

    //Recent events notification template (error, info, debug) 
    var menuDefaultTemplate =
        {
            priority: 1000,
            satisfy: function (notification, place) { return place == 'menu'; },
            //template for display that notification in menu and list
            template: '$(Platform)/Scripts/app/pushNotifications/menuDefault.tpl.html',
            //action executed when notification selected
            action: function (notify) { $state.go('workspace.pushNotificationsHistory', notify) }
        };

    //In history list notification template (error, info, debug)
    var historyDefaultTemplate =
        {
            priority: 1000,
            satisfy: function (notification, place) { return place == 'history'; },
            //template for display that notification in menu and list
            template: '$(Platform)/Scripts/app/pushNotifications/blade/historyDefault.tpl.html',
            //action executed in event detail
            action: function (notify) {
                var blade = {
                    id: 'notifyDetail',
                    title: 'platform.blades.historyDetailDefault.title',
                    subtitle: 'platform.blades.historyDetailDefault.subtitle',
                    template: '$(Platform)/Scripts/app/pushNotifications/blade/historyDetailDefault.tpl.html',
                    isClosingDisabled: false,
                    notify: notify
                };
                bladeNavigationService.showBlade(blade);
            }
        };

    retVal.register(menuDefaultTemplate);
    retVal.register(historyDefaultTemplate);

    return retVal;
}])
.factory('platformWebApp.pushNotificationService', ['$rootScope', '$timeout', '$interval', '$state', 'platformWebApp.mainMenuService', 'platformWebApp.pushNotificationTemplateResolver', 'platformWebApp.pushNotifications',
    function ($rootScope, $timeout, $interval, $state, mainMenuService, eventTemplateResolver, notifications) {

        //SignalR setup connection
        var connection = new signalR.HubConnectionBuilder()
            .withUrl("/pushNotificationHub")
            .build();     
        connection.start();
      
        connection.on('Send', function (data) {
            var notifyMenu = mainMenuService.findByPath('pushNotifications');
            var notificationTemplate = eventTemplateResolver.resolve(data, 'menu');
            //broadcast event
            $rootScope.$broadcast("new-notification-event", data);

            var menuItem = {
                path: 'pushNotifications/notifications',
                icon: 'fa fa-bell-o',
                title: data.title,
                priority: 2,
                permission: '',
                children: [],
                action: notificationTemplate.action,
                template: notificationTemplate.template,
                notify: data
            };

            var alreadyExitstItem = _.find(notifyMenu.children, function (x) { return x.notify.id == menuItem.notify.id; });
            if (alreadyExitstItem) {
                angular.copy(menuItem, alreadyExitstItem);
            }
            else {
                menuItem.parent = notifyMenu;
                notifyMenu.children.push(menuItem);
                notifyMenu.newCount++;

                if (angular.isDefined(notifyMenu.intervalPromise)) {
                    $interval.cancel(notifyMenu.intervalPromise);
                }
                animateNotify();
                notifyMenu.intervalPromise = $interval(animateNotify, 30000);
            }
        });

        function animateNotify() {
            var notifyMenu = mainMenuService.findByPath('pushNotifications');
            notifyMenu.isAnimated = true;

            $timeout(function () {
                notifyMenu.isAnimated = false;
            }, 1500);
        }

        function markAllAsRead() {
            var notifyMenu = mainMenuService.findByPath('pushNotifications');
            if (angular.isDefined(notifyMenu.intervalPromise)) {
                $interval.cancel(notifyMenu.intervalPromise);
            }

            notifications.markAllAsRead(null, function (data, status, headers, config) {
                notifyMenu.isAnimated = false;
                notifyMenu.newCount = 0;
            }, function (error) {
                //bladeNavigationService.setError('Error ' + error.status, blade);
            });
        }

        var retVal = {
            run: function () {
                if (!this.running) {
                    var notifyMenu = mainMenuService.findByPath('pushNotifications');
                    if (!angular.isDefined(notifyMenu)) {
                        notifyMenu = {
                            path: 'pushNotifications',
                            icon: 'fa fa-bell-o',
                            title: 'platform.menu.notifications',
                            priority: 2,
                            isAlwaysOnBar: true,
                            permission: '',
                            headerTemplate: '$(Platform)/Scripts/app/pushNotifications/menuHeader.tpl.html',
                            template: '$(Platform)/Scripts/app/pushNotifications/menu.tpl.html',
                            action: function () { markAllAsRead(); if (this.children.length == 0) { this.showHistory(); } },
                            showHistory: function () { $state.go('workspace.pushNotificationsHistory'); },
                            clearRecent: function () { notifyMenu.children.splice(0, notifyMenu.children.length); },
                            children: [],
                            newCount: 0
                        };
                        mainMenuService.addMenuItem(notifyMenu);
                    }
                    this.running = true;
                };
            },
            running: false
        };
        return retVal;

    }]);
