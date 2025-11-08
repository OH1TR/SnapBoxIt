import { createRouter, createWebHistory } from 'vue-router';
import MainPage from '../views/MainPage.vue';
import UploadPage from '../views/UploadPage.vue';
import SearchPage from '../views/SearchPage.vue';
import BoxViewPage from '../views/BoxViewPage.vue';
import PrintPage from '../views/PrintPage.vue';
import SettingsPage from '../views/SettingsPage.vue';

const routes = [
  {
    path: '/',
    name: 'Home',
    component: MainPage
  },
  {
    path: '/upload',
    name: 'Upload',
    component: UploadPage
  },
  {
    path: '/search',
    name: 'Search',
    component: SearchPage
  },
  {
    path: '/boxes',
    name: 'BoxView',
    component: BoxViewPage
  },
  {
    path: '/print',
    name: 'Print',
    component: PrintPage
  },
  {
    path: '/settings',
    name: 'Settings',
    component: SettingsPage
  }
];

const router = createRouter({
  history: createWebHistory('/SnapBoxPWA/'),
  routes
});

export default router;
