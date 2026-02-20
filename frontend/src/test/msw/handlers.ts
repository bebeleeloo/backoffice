import { authHandlers } from "./handlers/auth";
import { usersHandlers } from "./handlers/users";
import { rolesHandlers } from "./handlers/roles";
import { auditHandlers } from "./handlers/audit";
import { clientsHandlers } from "./handlers/clients";
import { countriesHandlers } from "./handlers/countries";
import { permissionsHandlers } from "./handlers/permissions";

export const handlers = [
  ...authHandlers,
  ...usersHandlers,
  ...rolesHandlers,
  ...auditHandlers,
  ...clientsHandlers,
  ...countriesHandlers,
  ...permissionsHandlers,
];
