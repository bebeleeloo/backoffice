import { authHandlers } from "./handlers/auth";
import { menuHandlers } from "./handlers/menu";
import { entitiesHandlers } from "./handlers/entities";
import { upstreamsHandlers } from "./handlers/upstreams";
import { permissionsHandlers } from "./handlers/permissions";
import { entityChangesHandlers } from "./handlers/entityChanges";

export const handlers = [
  ...authHandlers,
  ...menuHandlers,
  ...entitiesHandlers,
  ...upstreamsHandlers,
  ...permissionsHandlers,
  ...entityChangesHandlers,
];
