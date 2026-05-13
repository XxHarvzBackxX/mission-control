import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./components/mission-list/mission-list.component').then(m => m.MissionListComponent) },
  { path: 'missions/new', loadComponent: () => import('./components/mission-form/mission-form.component').then(m => m.MissionFormComponent) },
  { path: 'missions/:id/edit', loadComponent: () => import('./components/mission-form/mission-form.component').then(m => m.MissionFormComponent) },
  { path: 'missions/:id', loadComponent: () => import('./components/mission-summary/mission-summary.component').then(m => m.MissionSummaryComponent) },
  { path: 'rockets', loadComponent: () => import('./components/rocket-list/rocket-list.component').then(m => m.RocketListComponent) },
  { path: 'rockets/new', loadComponent: () => import('./components/rocket-builder/rocket-builder.component').then(m => m.RocketBuilderComponent) },
  { path: 'rockets/:id/edit', loadComponent: () => import('./components/rocket-builder/rocket-builder.component').then(m => m.RocketBuilderComponent) },
  { path: 'rockets/:id', loadComponent: () => import('./components/rocket-detail/rocket-detail.component').then(m => m.RocketDetailComponent) },
];
