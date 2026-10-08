import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { DashboardPage } from './pages/DashboardPage'
import { DocketPage } from './pages/DocketPage'
import { MatterDetailPage } from './pages/MatterDetailPage'
import { MattersPage } from './pages/MattersPage'
import { NewMatterPage } from './pages/NewMatterPage'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<DashboardPage />} />
          <Route path="docket" element={<DocketPage />} />
          <Route path="matters" element={<MattersPage />} />
          <Route path="matters/new" element={<NewMatterPage />} />
          <Route path="matters/:id" element={<MatterDetailPage />} />
          <Route path="*" element={<p>Page not found.</p>} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
