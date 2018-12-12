import { createStore, applyMiddleware } from "redux";
import thunkMiddleware from "redux-thunk";
import { createLogger } from "redux-logger";
import rootReducer from "../reducer";

const loggerMiddleware = createLogger();

export const store = createStore(
  rootReducer,
  {},
  applyMiddleware(thunkMiddleware, loggerMiddleware)
);

store.dispatch({ type: "ALERT_SUCCESS", message: "Voce entrou" });
