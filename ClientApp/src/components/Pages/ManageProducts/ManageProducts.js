import React, { Component } from "react";
import { Link } from "react-router-dom";
import { Container } from "semantic-ui-react";
import "./ManageProducts.css";

export class ManageProducts extends Component {
  constructor() {
    super();
    fetch("/api/Product/GetAdminProducts")
      .then(response => response.json())
      .then(data => {
        this.setState({
          ...this.state,
          movies: data.items,
          isLoading: "Not needed"
        });
        });
    this.state = { isLoading: "isLoading", movies: [] };
    
  }

  renderProductTable() {
    return (
      <table className="table">
        <thead>
          <tr>
            <th scope="col">Id</th>
            <th scope="col">Titel</th>
            <th scope="col">Prijs</th>
            <th scope="col">Voorraad</th>
            <th scope="col"></th>
          </tr>
        </thead>
        <tbody>
          {this.state.movies.map(movie => (
            <tr key={movie.id}>
              <td>{movie.id}</td>
              <td>{movie.title}</td>
              <td>{movie.price}</td>
              <td>{movie.quantity}</td>
              <td>                    
                <Link className="nav-link" to={"/updateproduct/" + movie.id}>
                  Update
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    );
  }

  render() {
    let contents = this.renderProductTable();
    return (
      <Container style={{ marginTop: "7em" }}>
        <h1>Admin</h1>
        <Link className="nav-link" to={"/addproduct"}>
          Nieuw product
        </Link>
        {' | '}
        <Link className="nav-link" to={"/addcategory"}>
          Nieuw categorie
        </Link>
        {contents}
      </Container>
    );
  }
}
