import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  scenarios: {
    piloto_10_usuarios: {
      executor: "constant-vus",
      vus: 10,
      duration: "2m",
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(95)<3000"],
    checks: ["rate>0.99"],
  },
};

const baseUrl = __ENV.BASE_URL;
const token = __ENV.ACCESS_TOKEN;

export function setup() {
  if (!baseUrl || !token) {
    throw new Error("Configure BASE_URL y ACCESS_TOKEN de un usuario de prueba.");
  }
}

export default function () {
  const headers = {
    Authorization: `Bearer ${token}`,
    "X-Correlation-Id": `k6-${__VU}-${__ITER}`,
  };

  const perfil = http.get(`${baseUrl}/api/v1/perfil`, { headers });
  check(perfil, {
    "perfil responde 200": (r) => r.status === 200,
    "no expone detalles internos": (r) => !r.body.includes("System."),
  });

  const movimientos = http.get(`${baseUrl}/api/v1/movimientos?limite=20`, { headers });
  check(movimientos, {
    "movimientos responde 200": (r) => r.status === 200,
  });

  sleep(1);
}
