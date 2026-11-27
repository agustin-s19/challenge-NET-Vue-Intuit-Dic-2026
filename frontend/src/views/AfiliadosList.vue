<template>
  <div>
    <h2>Afiliados</h2>

    <label class="toggle-bajas">
      <input type="checkbox" v-model="mostrarBajas" @change="cargarAfiliados" />
      Mostrar dados de baja
    </label>

    <table v-if="afiliados.length">
      <thead>
        <tr>
          <th>#</th>
          <th>Nombre</th>
          <th>DNI</th>
          <th>Email</th>
          <th>Teléfono</th>
          <th>Plan</th>
          <th>Consultas usadas</th>
          <th>Bloqueado</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="a in afiliados" :key="a.id" :class="{ 'fila-baja': !a.isActive }">
          <td>{{ a.id }}</td>
          <td>
            <router-link :to="{ path: '/consultas', query: { afiliadoId: a.id } }">{{ a.nombreCompleto }}</router-link>
            <span v-if="!a.isActive" class="badge-baja">Baja</span>
          </td>
          <td>{{ a.dni }}</td>
          <td>{{ a.email }}</td>
          <td>{{ a.telefono }}</td>
          <td>{{ a.plan }}</td>
          <td :class="{ 'limite-alcanzado': a.consultasUsadas >= 6 }">
            {{ a.consultasUsadas }} / 6
          </td>
          <td>
            <span v-if="a.bloqueado" class="bloqueado-si">Sí</span>
            <span v-else class="bloqueado-no">No</span>
          </td>
          <td>
            <div class="acciones">
              <template v-if="a.isActive">
                <button v-if="a.bloqueado" @click="desbloquear(a)">Desbloquear</button>
                <button class="btn-danger" @click="eliminar(a)">Eliminar</button>
              </template>
              <button v-else @click="reactivar(a)">Reactivar</button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else>No hay afiliados registrados.</p>
  </div>
</template>

<script>
import { afiliadosApi } from '../services/api'

function mensajeError(err) {
  return err.response?.data?.message
    || err.response?.data?.mensaje
    || 'Ocurrió un error inesperado. Intente nuevamente.'
}

export default {
  name: 'AfiliadosList',
  data() {
    return {
      afiliados: [],
      mostrarBajas: false
    }
  },
  async mounted() {
    await this.cargarAfiliados()
  },
  methods: {
    async cargarAfiliados() {
      try {
        const res = await afiliadosApi.getAll(this.mostrarBajas)
        this.afiliados = res.data
      } catch (err) {
        alert(mensajeError(err))
      }
    },
    async desbloquear(afiliado) {
      if (!confirm(`¿Desbloquear a ${afiliado.nombreCompleto}?`)) return
      try {
        await afiliadosApi.unlock(afiliado.id)
      } catch (err) {
        alert(mensajeError(err))
        return
      }
      alert(`${afiliado.nombreCompleto} fue desbloqueado.`)
      await this.cargarAfiliados()
    },
    async eliminar(afiliado) {
      if (!confirm(`¿Dar de baja a ${afiliado.nombreCompleto}? Su historial de consultas se conserva.`)) return
      try {
        await afiliadosApi.delete(afiliado.id)
      } catch (err) {
        alert(mensajeError(err))
        return
      }
      alert(`${afiliado.nombreCompleto} fue dado de baja.`)
      await this.cargarAfiliados()
    },
    async reactivar(afiliado) {
      if (!confirm(`¿Reactivar a ${afiliado.nombreCompleto}?`)) return
      try {
        await afiliadosApi.reactivate(afiliado.id)
      } catch (err) {
        alert(mensajeError(err))
        return
      }
      alert(`${afiliado.nombreCompleto} fue reactivado.`)
      await this.cargarAfiliados()
    }
  }
}
</script>

<style scoped>
.toggle-bajas {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 12px;
  font-size: 14px;
  color: #555;
}
.limite-alcanzado {
  color: #b3261e;
  font-weight: 600;
}
.bloqueado-si {
  color: #d32f2f;
  font-weight: 600;
}
.bloqueado-no {
  color: #388e3c;
}
.acciones {
  display: flex;
  gap: 8px;
  white-space: nowrap;
}
.fila-baja td {
  color: #9e9e9e;
}
.fila-baja .bloqueado-si,
.fila-baja .bloqueado-no,
.fila-baja .limite-alcanzado {
  color: #9e9e9e;
}
.fila-baja .acciones button {
  color: initial;
}
.badge-baja {
  margin-left: 6px;
  padding: 1px 8px;
  border-radius: 10px;
  font-size: 11px;
  font-weight: 600;
  background: #eceff1;
  color: #546e7a;
}
</style>