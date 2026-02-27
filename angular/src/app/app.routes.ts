import { authGuard, permissionGuard } from '@abp/ng.core';
import { Routes } from '@angular/router';
import { eLayoutType } from '@abp/ng.core';

export const APP_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./home/home.component').then(c => c.HomeComponent),
  },
  {
    path: 'account',
    loadChildren: () => import('@abp/ng.account').then(c => c.createRoutes()),
  },
  {
    path: 'identity',
    loadChildren: () => import('@abp/ng.identity').then(c => c.createRoutes()),
  },
  {
    path: 'tenant-management',
    loadChildren: () => import('@abp/ng.tenant-management').then(c => c.createRoutes()),
  },
  {
    path: 'setting-management',
    loadChildren: () => import('@abp/ng.setting-management').then(c => c.createRoutes()),
  },
  {
    path: 'tasks',
    loadChildren: () => import('./tasks/tasks-module').then(m => m.TasksModule),
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'TaskManagement.Tasks',
    },
  },
  {
    path: 'projects',
    loadChildren: () => import('./projects/projects-module').then(m => m.ProjectsModule),
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'TaskManagement.Projects',
    },
  },
  {
    path: 'calendars',
    loadChildren: () => import('./calendars/calendars-module').then(m => m.CalendarsModule),
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'TaskManagement.Calendars',
    },
  },
];
